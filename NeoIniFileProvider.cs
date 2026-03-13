using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comments = System.Collections.Generic.List<NeoIni.Comment>;
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

    private readonly string FilePath;
    private readonly byte[] EncryptionKey;
    private readonly byte[] Salt;
    private readonly bool AutoEncryption = false;

    private string TempFilePath => FilePath + ".tmp";
    private string BackupFilePath => FilePath + ".backup";

    internal readonly bool Encryption = false;

    internal event EventHandler<ErrorEventArgs> Error;
    internal event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch;

    internal NeoIniFileProvider(string filePath) => FilePath = filePath;

    internal NeoIniFileProvider(string filePath, EncryptionParameters encryptionParameters, bool autoModeEncryption)
    {
        FilePath = filePath;
        EncryptionKey = encryptionParameters.Key;
        Salt = encryptionParameters.Salt;
        Encryption = true;
        AutoEncryption = autoModeEncryption;
    }

    internal void DeleteBackup() { if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath); }

    internal void DeleteFile()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
    }

    internal NeoIniData GetData(bool humanization = false)
    {
        Data data = new();
        Comments comments = new();
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return new(data, comments);
        }
        string currentSection = null;
        var lines = ReadFile();
        if (lines == null) return new(data, comments);
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim(' ', '\t', '\u00A0', '\u200B');
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (NeoIniParser.IsCommentLine(trimmed))
            {
                NeoIniParser.HandleCommentLine(lines, i, trimmed, humanization, comments);
                continue;
            }
            if (NeoIniParser.IsSectionLine(trimmed))
            {
                currentSection = NeoIniParser.HandleSectionLine(trimmed, humanization, data, comments);
                continue;
            }
            if (currentSection != null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string key, out string value))
                NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
        }
        return new(data, comments);
    }

    internal async Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default)
    {
        Data data = new();
        Comments comments = new();
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return new(data, comments);
        }
        var lines = await ReadFileAsync(ct).ConfigureAwait(false);
        if (lines == null) return new(data, comments);
        string currentSection = null;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (NeoIniParser.IsCommentLine(trimmed))
            {
                NeoIniParser.HandleCommentLine(lines, i, trimmed, humanization, comments);
                continue;
            }
            if (NeoIniParser.IsSectionLine(trimmed))
            {
                currentSection = NeoIniParser.HandleSectionLine(trimmed, humanization, data, comments);
                continue;
            }
            if (currentSection != null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string key, out string value))
                NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
        }
        return new(data, comments);
    }

    internal void SaveFile(string content, bool useChecksum, bool useBackup)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        byte[] dataWithChecksum;
        try
        {
            byte[] header = BuildHeader(useChecksum);
            if (!Encryption)
            {
                using MemoryStream ms = new(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
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
                using MemoryStream ms = new();
                ms.Write(header, 0, header.Length);
                if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);
                ms.Write(Salt, 0, Salt.Length);
                using var encryptor = aes.CreateEncryptor();
                using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write, leaveOpen: true))
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
            if (Error != null) RaiseError(this, new(ex));
            else throw;
        }
    }

    internal async Task SaveFileAsync(string content, bool useChecksum, bool useBackup, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        byte[] dataWithChecksum;
        try
        {
            byte[] header = BuildHeader(useChecksum);
            ct.ThrowIfCancellationRequested();
            if (!Encryption)
            {
                using MemoryStream ms = new(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
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
                await using MemoryStream ms = new();
                await ms.WriteAsync(header, 0, header.Length, ct).ConfigureAwait(false);
                if (useChecksum) await ms.WriteAsync(WarningBytes, 0, WarningBytes.Length, ct).ConfigureAwait(false);
                await ms.WriteAsync(aes.IV, 0, aes.IV.Length, ct).ConfigureAwait(false);
                await ms.WriteAsync(Salt, 0, Salt.Length, ct).ConfigureAwait(false);
                using var encryptor = aes.CreateEncryptor();
                await using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write, leaveOpen: true))
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
            if (Error != null) RaiseError(this, new(ex));
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
        if (Encryption) flags |= HeaderFlags.IsEncrypted;
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

    private EncryptionParameters GetEncryptionParameters(string path, bool autoModeEncryption)
    {
        if (EncryptionKey == null)
        {
            if (autoModeEncryption) return NeoIniEncryptionProvider.GetEncryptionParameters(salt: GetSalt(path));
            else throw new MissingEncryptionKeyException();
        }
        else
        {
            if (!AutoEncryption && autoModeEncryption)
                return NeoIniEncryptionProvider.GetEncryptionParameters(salt: GetSalt(path));
            return new(EncryptionKey.Clone() as byte[], Salt.Clone() as byte[]);
        }
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
            if (!TryParseHeader(fileBytes, out var headerParameters))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            int minLength = headerParameters.HeaderLength +
                (headerParameters.HasChecksum ? WarningBytes.Length + ChecksumSize : 0) +
                (headerParameters.IsEncrypted ? IvSize + SaltSize : 0);
            if (fileBytes.Length < minLength)
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            if (!ValidateChecksum(fileBytes, headerParameters.HasChecksum))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            int index = headerParameters.HeaderLength;
            if (headerParameters.HasChecksum) index += WarningBytes.Length;
            string content;
            if (!headerParameters.IsEncrypted)
            {
                int dataLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (dataLength <= 0) return null;
                content = Encoding.UTF8.GetString(fileBytes, index, dataLength);
                return SplitLines(content);
            }
            else
            {
                var encryptionParameters = GetEncryptionParameters(path, headerParameters.AutoModeEncryption);
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                byte[] encryptedContent = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedContent, 0, encryptedLength);
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionParameters.Key;
                aes.IV = iv;
                using MemoryStream ms = new(encryptedContent);
                using var decryptor = aes.CreateDecryptor();
                using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader sr = new(cs, Encoding.UTF8);
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
            RaiseError(this, new(ex));
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
            if (!TryParseHeader(fileBytes, out var headerParameters))
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            int minLength = headerParameters.HeaderLength +
                (headerParameters.HasChecksum ? WarningBytes.Length + ChecksumSize : 0) +
                (headerParameters.IsEncrypted ? IvSize + SaltSize : 0);
            if (fileBytes.Length < minLength)
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            if (!ValidateChecksum(fileBytes, headerParameters.HasChecksum))
            {
                if (isBackup) return null;
                return await CheckBackupAsync(ct).ConfigureAwait(false);
            }
            int index = headerParameters.HeaderLength;
            if (headerParameters.HasChecksum) index += WarningBytes.Length;
            if (!headerParameters.IsEncrypted)
            {
                int dataLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (dataLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                string content = Encoding.UTF8.GetString(fileBytes, index, dataLength);
                return SplitLines(content);
            }
            else
            {
                var encryptionParameters = GetEncryptionParameters(path, headerParameters.AutoModeEncryption);
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                byte[] encryptedContent = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedContent, 0, encryptedLength);
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionParameters.Key;
                aes.IV = iv;
                using MemoryStream ms = new(encryptedContent);
                using var decryptor = aes.CreateDecryptor();
                using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader sr = new(cs, Encoding.UTF8);
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
            RaiseError(this, new(ex));
            return await CheckBackupAsync(ct).ConfigureAwait(false);
        }
    }

    internal static byte[] GetSalt(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] fileBytes = NeoIniIO.ReadAllBytes(path);
        if (!TryParseHeader(fileBytes, out var headerParameters)) return null;
        if (!headerParameters.IsEncrypted) return null;
        if (!TryReadSalt(fileBytes, headerParameters.HeaderLength, headerParameters.HasChecksum, out byte[] salt)) return null;
        return salt;
    }

    private static bool TryReadSalt(byte[] fileBytes, int headerLength, bool hasChecksum, out byte[] salt)
    {
        int start = headerLength + (hasChecksum ? WarningBytes.Length : 0) + IvSize;
        if (fileBytes.Length < start + SaltSize) { salt = null; return false; }
        salt = fileBytes[start..(start + SaltSize)];
        return true;
    }

    private static bool TryParseHeader(byte[] fileBytes, out HeaderParameters headerParameters)
    {
        headerParameters = null;
        if (fileBytes.Length < HeaderSize) return false;
        if (!fileBytes.AsSpan(0, FileSignature.Length).SequenceEqual(FileSignature)) return false;
        byte version = fileBytes[4];
        if (version != FileVersion) return false;
        headerParameters = new((HeaderFlags)fileBytes[5]) { HeaderLength = HeaderSize };
        return true;
    }

    private bool ValidateChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return true;
        if (data.Length < ChecksumSize) return false;
        ReadOnlySpan<byte> dataSpan = data.AsSpan()[..^ChecksumSize];
        ReadOnlySpan<byte> expectedChecksum = data.AsSpan()[^ChecksumSize..];
        byte[] calculatedChecksum = SHA256.HashData(dataSpan);
        bool isValid = expectedChecksum.SequenceEqual(calculatedChecksum);
        if (!isValid) ChecksumMismatch?.Invoke(this, new(calculatedChecksum, expectedChecksum.ToArray()));
        return isValid;
    }

    internal byte[] GetFileChecksum()
    {
        var data = NeoIniIO.ReadAllBytes(FilePath);
        return NeoIniEncryptionProvider.HashData(data);
    }

    internal void RaiseError(object sender, ErrorEventArgs e) => Error?.Invoke(sender ?? this, e);
}
