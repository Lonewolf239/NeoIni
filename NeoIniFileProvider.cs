using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniFileProvider
{
    internal sealed class MissingEncryptionKeyException : Exception
    {
        public MissingEncryptionKeyException()
            : base("The configuration file is encrypted with a custom password. Please provide a password string to the NeoIniReader constructor.") { }
    }

    private const byte FileVersion = 1;
    private const int HeaderSize = 10;
    private const int ReservedSize = 2;
    private const int IvSize = 16;
    private const int SaltSize = 16;
    private const int ChecksumSize = 32;
    private const string WarningText = "; WARNING: This file is auto-generated.\n; Any manual changes will be overwritten and may cause data loss.\n; The original data will be restored from backup.\n";

    private static readonly byte[] FileSignature = { (byte)'N', (byte)'I', (byte)'N', (byte)'I' };
    private static readonly byte[] WarningBytes = Encoding.UTF8.GetBytes(WarningText);
    private static readonly string[] LineSeparators = new[] { "\r\n", "\n", "\r" };

    [Flags]
    private enum HeaderFlags : byte
    {
        None = 0,
        HasChecksum = 1 << 0,
        IsEncrypted = 1 << 1,
        AutoMode = 1 << 2,
        CustomMode = 1 << 3,
        HasSalt = 1 << 4
    }

    private readonly string FilePath;
    private readonly byte[] EncryptionKey;
    private readonly byte[] Salt;
    private readonly bool Encryption = false;
    private readonly bool AutoEncryption = false;

    private string TempFilePath => FilePath + ".tmp";
    private string BackupFilePath => FilePath + ".backup";

    internal Action<Exception> OnError;
    internal Action<byte[], byte[]> OnChecksumMismatch;

    internal NeoIniFileProvider(string filePath) => FilePath = filePath;

    internal NeoIniFileProvider(string filePath, (byte[] key, byte[] salt) encryptionData, bool autoModeEncryption)
    {
        FilePath = filePath;
        EncryptionKey = encryptionData.key;
        Salt = encryptionData.salt;
        Encryption = true;
        AutoEncryption = autoModeEncryption;
    }

    internal void DeleteBackup() { if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath); }

    internal void DeleteFile()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
    }

    internal Data GetData()
    {
        var data = new Data();
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return data;
        }
        string currentSection = null;
        var lines = ReadFile();
        if (lines == null) return data;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (trimmed.StartsWith(';')) continue;
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed.Trim('[', ']');
                if (!data.ContainsKey(currentSection)) data[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string key, out string value))
                data[currentSection][key] = value;
        }
        return data;
    }

    internal async Task<Data> GetDataAsync(CancellationToken ct = default)
    {
        var data = new Data();
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return data;
        }
        var lines = await ReadFileAsync(ct).ConfigureAwait(false);
        if (lines == null) return data;
        string currentSection = null;
        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (trimmed.StartsWith(';')) continue;
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed.Trim('[', ']');
                if (!data.ContainsKey(currentSection))
                    data[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out var key, out var value))
                data[currentSection][key] = value;
        }
        return data;
    }

    internal void SaveFile(string content, bool useChecksum, bool useBackup)
    {
        if (string.IsNullOrEmpty(content)) return;
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        try
        {
            byte[] header = BuildHeader(useChecksum);
            if (!Encryption)
            {
                using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
                ms.Write(header, 0, header.Length);
                if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
                ms.Write(plaintextBytes, 0, plaintextBytes.Length);
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                NeoIniIO.WriteBytes(TempFilePath, dataWithChecksum);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                using var ms = new MemoryStream();
                ms.Write(header, 0, header.Length);
                if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);
                ms.Write(Salt, 0, Salt.Length);
                using var encryptor = aes.CreateEncryptor();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                    cs.FlushFinalBlock();
                }
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                NeoIniIO.WriteBytes(TempFilePath, dataWithChecksum);
            }
            if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, useBackup ? BackupFilePath : null);
            else File.Move(TempFilePath, FilePath);
        }
        catch (Exception ex)
        {
            if (OnError != null) OnError.Invoke(ex);
            else throw;
        }
    }

    internal async Task SaveFileAsync(string content, bool useChecksum, bool useBackup, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(content)) return;
        ct.ThrowIfCancellationRequested();
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        try
        {
            byte[] header = BuildHeader(useChecksum);
            ct.ThrowIfCancellationRequested();
            if (!Encryption)
            {
                using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
                ms.Write(header, 0, header.Length);
                if (useChecksum) await ms.WriteAsync(WarningBytes, 0, WarningBytes.Length, ct).ConfigureAwait(false);
                await ms.WriteAsync(plaintextBytes, 0, plaintextBytes.Length, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                await using var ms = new MemoryStream();
                await ms.WriteAsync(header, 0, header.Length, ct).ConfigureAwait(false);
                if (useChecksum) await ms.WriteAsync(WarningBytes, 0, WarningBytes.Length, ct).ConfigureAwait(false);
                await ms.WriteAsync(aes.IV, 0, aes.IV.Length, ct).ConfigureAwait(false);
                await ms.WriteAsync(Salt, 0, Salt.Length, ct).ConfigureAwait(false);
                using var encryptor = aes.CreateEncryptor();
                await using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length, ct).ConfigureAwait(false);
                    await cs.FlushFinalBlockAsync().ConfigureAwait(false);
                }
                ct.ThrowIfCancellationRequested();
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, useBackup ? BackupFilePath : null);
            else File.Move(TempFilePath, FilePath);
        }
        catch (Exception ex)
        {
            if (OnError != null) OnError.Invoke(ex);
            else throw;
        }
    }

    private static string[] SplitLines(string content) => content.Split(LineSeparators, StringSplitOptions.None);

    private byte[] AddChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return data;
        byte[] dataWithChecksum = new byte[data.Length + ChecksumSize];
        Array.Copy(data, dataWithChecksum, data.Length);
        byte[] checksum = SHA256.HashData(data);
        Array.Copy(checksum, 0, dataWithChecksum, data.Length, ChecksumSize);
        return dataWithChecksum;
    }

    private byte[] BuildHeader(bool useChecksum)
    {
        var flags = HeaderFlags.None;
        if (useChecksum) flags |= HeaderFlags.HasChecksum;
        if (Encryption) flags |= HeaderFlags.IsEncrypted | HeaderFlags.HasSalt;
        if (AutoEncryption) flags |= HeaderFlags.AutoMode;
        else flags |= HeaderFlags.CustomMode;
        byte[] header = new byte[HeaderSize];
        Array.Copy(FileSignature, 0, header, 0, FileSignature.Length);
        header[4] = FileVersion;
        header[5] = (byte)flags;
        header[6] = 0;
        header[7] = 0;
        header[8] = 0x0D;
        header[9] = 0x0A;
        return header;
    }

    private string[] CheckBackup()
    {
        if (!File.Exists(BackupFilePath)) return null;
        return ReadFile(BackupFilePath, true);
    }

    private async Task<string[]> CheckBackupAsync(CancellationToken ct)
    {
        if (!File.Exists(BackupFilePath)) return null;
        return await ReadFileAsync(BackupFilePath, true, ct);
    }

    private (byte[], byte[]) GetEffectiveEncryptionKeyAndSalt(bool autoModeEncryption)
    {
        if (EncryptionKey == null)
        {
            if (autoModeEncryption) return NeoIniEncryptionProvider.GetEncryptionKeyAndSalt(salt: GetSalt(FilePath));
            else throw new MissingEncryptionKeyException();
        }
        else return (EncryptionKey.Clone() as byte[], Salt.Clone() as byte[]);
    }

    private string[] ReadFile() => ReadFile(FilePath, false);

    private string[] ReadFile(string path, bool isBackup)
    {
        if (!File.Exists(path))
        {
            if (isBackup) return null;
            return CheckBackup();
        }
        try
        {
            byte[] fileBytes = NeoIniIO.ReadAllBytes(path);
            if (!TryParseHeader(fileBytes, out int headerLength, out bool hasChecksum, out bool isEncrypted, out bool autoModeEncryption, out _))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            int minLength = headerLength + (hasChecksum ? WarningBytes.Length + ChecksumSize : 0) + (isEncrypted ? IvSize : 0);
            if (fileBytes.Length < minLength)
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            if (!ValidateChecksum(fileBytes, hasChecksum))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            int index = headerLength;
            if (hasChecksum) index += WarningBytes.Length;
            string content;
            if (!isEncrypted)
            {
                int dataLength = fileBytes.Length - index - (hasChecksum ? ChecksumSize : 0);
                if (dataLength <= 0) return null;
                content = Encoding.UTF8.GetString(fileBytes, index, dataLength);
                return SplitLines(content);
            }
            else
            {
                (byte[] encryptionKey, _) = GetEffectiveEncryptionKeyAndSalt(autoModeEncryption);
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (hasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                byte[] encryptedContent = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedContent, 0, encryptedLength);
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;
                using var ms = new MemoryStream(encryptedContent);
                using var decryptor = aes.CreateDecryptor();
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                content = sr.ReadToEnd();
                return SplitLines(content);
            }
        }
        catch (CryptographicException ex)
        {
            if (isBackup) return null;
            var data = CheckBackup();
            if (data != null) return data;
            throw new InvalidOperationException("Failed to decrypt configuration file.\n" +
                    "Check that you are using the same encryption password or environment as during file creation", ex);
        }
        catch (MissingEncryptionKeyException)
        {
            if (isBackup) return null;
            var data = CheckBackup();
            if (data != null) return data;
            throw;
        }
        catch (Exception ex)
        {
            if (isBackup) return null;
            OnError?.Invoke(ex);
            return CheckBackup();
        }
    }

    private async Task<string[]> ReadFileAsync(CancellationToken ct) => await ReadFileAsync(FilePath, false, ct);

    private async Task<string[]> ReadFileAsync(string path, bool isBackup, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            if (isBackup) return null;
            return await CheckBackupAsync(ct).ConfigureAwait(false);
        }
        try
        {
            byte[] fileBytes = await NeoIniIO.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            if (!TryParseHeader(fileBytes, out int headerLength, out bool hasChecksum, out bool isEncrypted, out bool autoModeEncryption, out _))
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            int minLength = headerLength + (hasChecksum ? WarningBytes.Length + ChecksumSize : 0) + (isEncrypted ? IvSize : 0);
            if (fileBytes.Length < minLength)
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            if (!ValidateChecksum(fileBytes, hasChecksum))
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            int index = headerLength;
            if (hasChecksum) index += WarningBytes.Length;
            if (!isEncrypted)
            {
                int dataLength = fileBytes.Length - index - (hasChecksum ? ChecksumSize : 0);
                if (dataLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                string content = Encoding.UTF8.GetString(fileBytes, index, dataLength);
                return SplitLines(content);
            }
            else
            {
                (byte[] encryptionKey, _) = GetEffectiveEncryptionKeyAndSalt(autoModeEncryption);
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (hasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                byte[] encryptedContent = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedContent, 0, encryptedLength);
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;
                using var ms = new MemoryStream(encryptedContent);
                using var decryptor = aes.CreateDecryptor();
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                ct.ThrowIfCancellationRequested();
                string content = await sr.ReadToEndAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                return SplitLines(content);
            }
        }
        catch (CryptographicException ex)
        {
            if (isBackup) return null;
            var data = await CheckBackupAsync(ct).ConfigureAwait(false);
            if (data != null) return data;
            throw new InvalidOperationException("Failed to decrypt configuration file.\n" +
                    "Check that you are using the same encryption password or environment as during file creation", ex);
        }
        catch (MissingEncryptionKeyException)
        {
            if (isBackup) return null;
            var data = await CheckBackupAsync(ct).ConfigureAwait(false);
            if (data != null) return data;
            throw;
        }
        catch (Exception ex)
        {
            if (isBackup) return null;
            OnError?.Invoke(ex);
            return await CheckBackupAsync(ct).ConfigureAwait(false);
        }
    }

    internal static byte[] GetSalt(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] fileBytes = NeoIniIO.ReadAllBytes(path);
        if (!TryParseHeader(fileBytes, out int headerLength, out bool hasChecksum, out _, out _, out bool hasSalt)) return null;
        if (!hasSalt) return null;
        if (!TryReadSalt(fileBytes, headerLength, hasChecksum, out byte[] salt)) return null;
        return salt;
    }

    private static bool TryReadSalt(byte[] fileBytes, int headerLength, bool hasChecksum, out byte[] salt)
    {
        salt = null;
        int index = headerLength;
        if (hasChecksum) index += WarningBytes.Length;
        int required = index + IvSize + SaltSize;
        if (fileBytes.Length < required) return false;
        index += IvSize;
        salt = new byte[SaltSize];
        Array.Copy(fileBytes, index, salt, 0, SaltSize);
        index += SaltSize;
        return true;
    }

    private static bool TryParseHeader(byte[] fileBytes, out int headerLength, out bool hasChecksum,
            out bool isEncrypted, out bool autoModeEncryption, out bool hasSalt)
    {
        headerLength = 0;
        hasChecksum = false;
        isEncrypted = false;
        autoModeEncryption = false;
        hasSalt = false;
        if (fileBytes.Length < HeaderSize) return false;
        if (!fileBytes.AsSpan(0, FileSignature.Length).SequenceEqual(FileSignature)) return false;
        byte version = fileBytes[4];
        if (version != FileVersion) return false;
        var flags = (HeaderFlags)fileBytes[5];
        hasChecksum = flags.HasFlag(HeaderFlags.HasChecksum);
        isEncrypted = flags.HasFlag(HeaderFlags.IsEncrypted);
        autoModeEncryption = flags.HasFlag(HeaderFlags.AutoMode);
        hasSalt = flags.HasFlag(HeaderFlags.HasSalt);
        headerLength = HeaderSize;
        return true;
    }

    private bool ValidateChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return true;
        if (data.Length < ChecksumSize) return false;
        int dataSize = data.Length - ChecksumSize;
        byte[] dataWithOutChecksum = new byte[dataSize];
        byte[] checksumFromData = new byte[ChecksumSize];
        Array.Copy(data, 0, dataWithOutChecksum, 0, dataSize);
        Array.Copy(data, dataSize, checksumFromData, 0, ChecksumSize);
        byte[] calculatedChecksum = SHA256.HashData(dataWithOutChecksum);
        bool result = checksumFromData.SequenceEqual(calculatedChecksum);
        if (!result) OnChecksumMismatch?.Invoke(calculatedChecksum, checksumFromData);
        return result;
    }
}
