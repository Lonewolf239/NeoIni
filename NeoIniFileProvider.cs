using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniFileProvider
{
    private readonly string FilePath;
    private string TempFilePath => FilePath + ".tmp";
    private string BackupFilePath => FilePath + ".backup";
    private readonly byte[] EncryptionKey;
    private readonly bool AutoEncryption = false;
    private const int ChecksumSize = 32;
    private const string WarningText = "; WARNING: This file is auto-generated.\n; Any manual changes will be overwritten and may cause data loss.\n; The original data will be restored from backup.\n";
    internal Action<Exception> OnError;
    internal Action<byte[], byte[]> OnChecksumMismatch;

    internal NeoIniFileProvider(string filePath) => FilePath = filePath;

    internal NeoIniFileProvider(string filePath, byte[] encryptionKey)
    {
        FilePath = filePath;
        EncryptionKey = encryptionKey;
        AutoEncryption = true;
    }

    internal Data GetData(bool useChecksum)
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
        var lines = ReadFile(useChecksum);
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

    internal void DeleteFile()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
    }

    internal void DeleteBackup() { if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath); }

    private string[] CheckBackup(bool useChecksum)
    {
        if (!File.Exists(BackupFilePath)) return null;
        return ReadFile(BackupFilePath, useChecksum, true);
    }

    private string[] ReadFile(bool useChecksum) => ReadFile(FilePath, useChecksum, false);

    private string[] ReadFile(string path, bool useChecksum, bool isBackup)
    {
        if (!File.Exists(path))
        {
            if (isBackup) return null;
            return CheckBackup(useChecksum);
        }
        try
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            int headerLength = useChecksum ? Encoding.UTF8.GetByteCount(WarningText) : 0;
            if (fileBytes.Length < headerLength + (useChecksum ? ChecksumSize : 0) + (AutoEncryption ? 16 : 0))
            {
                if (isBackup) return null;
                return CheckBackup(useChecksum);
            }
            if (!ValidateChecksum(fileBytes, useChecksum))
            {
                if (isBackup) return null;
                return CheckBackup(useChecksum);
            }
            string content;
            int totalDataLength = useChecksum ? fileBytes.Length - ChecksumSize : fileBytes.Length;
            if (!AutoEncryption)
            {
                int contentLength = totalDataLength - headerLength;
                if (contentLength <= 0) return new string[0];
                content = Encoding.UTF8.GetString(fileBytes, headerLength, contentLength);
                return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            }
            else
            {
                byte[] iv = new byte[16];
                Array.Copy(fileBytes, headerLength, iv, 0, 16);
                int dataStartIndex = headerLength + 16;
                int encryptedLength = totalDataLength - dataStartIndex;
                if (encryptedLength <= 0) return new string[0];
                byte[] encryptedContent = new byte[encryptedLength];
                Array.Copy(fileBytes, dataStartIndex, encryptedContent, 0, encryptedLength);
                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(encryptedContent);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                content = sr.ReadToEnd();
                return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            }
        }
        catch (CryptographicException ex)
        {
            if (isBackup) return null;
            var data = CheckBackup(useChecksum);
            if (data != null) return data;
            throw new InvalidOperationException("Failed to decrypt configuration file.", ex);
        }
        catch (Exception ex)
        {
            if (isBackup) return null;
            OnError?.Invoke(ex);
            return CheckBackup(useChecksum);
        }
    }

    internal void SaveFile(string content, bool useChecksum, bool useBackup)
    {
        if (string.IsNullOrEmpty(content)) return;
        byte[] warningBytes = Encoding.UTF8.GetBytes(WarningText);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        try
        {
            if (!AutoEncryption)
            {
                using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? warningBytes.Length : 0));
                if (useChecksum) ms.Write(warningBytes, 0, warningBytes.Length);
                ms.Write(plaintextBytes, 0, plaintextBytes.Length);
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                File.WriteAllBytes(TempFilePath, dataWithChecksum);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();
                if (useChecksum) ms.Write(warningBytes, 0, warningBytes.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);
                using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                cs.FlushFinalBlock();
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                File.WriteAllBytes(TempFilePath, dataWithChecksum);
            }
            if (useBackup) File.Replace(TempFilePath, FilePath, BackupFilePath);
            else File.Replace(TempFilePath, FilePath, null);
        }
        catch (Exception ex) { OnError?.Invoke(ex); }
    }

    internal async Task SaveFileAsync(string content, bool useChecksum, bool useBackup)
    {
        if (string.IsNullOrEmpty(content)) return;
        byte[] warningBytes = Encoding.UTF8.GetBytes(WarningText);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        try
        {
            if (!AutoEncryption)
            {
                using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? warningBytes.Length : 0));
                if (useChecksum) await ms.WriteAsync(warningBytes, 0, warningBytes.Length).ConfigureAwait(false);
                await ms.WriteAsync(plaintextBytes, 0, plaintextBytes.Length).ConfigureAwait(false);
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await File.WriteAllBytesAsync(TempFilePath, dataWithChecksum).ConfigureAwait(false);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                using var encryptor = aes.CreateEncryptor();
                await using var ms = new MemoryStream();
                if (useChecksum) await ms.WriteAsync(warningBytes, 0, warningBytes.Length).ConfigureAwait(false);
                await ms.WriteAsync(aes.IV, 0, aes.IV.Length).ConfigureAwait(false);
                await using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length).ConfigureAwait(false);
                await cs.FlushFinalBlockAsync().ConfigureAwait(false);
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await File.WriteAllBytesAsync(TempFilePath, dataWithChecksum).ConfigureAwait(false);
            }
            if (useBackup) File.Replace(TempFilePath, FilePath, BackupFilePath);
            else File.Replace(TempFilePath, FilePath, null);
        }
        catch (Exception ex) { OnError?.Invoke(ex); }
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

    private byte[] AddChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return data;
        byte[] dataWithChecksum = new byte[data.Length + ChecksumSize];
        Array.Copy(data, dataWithChecksum, data.Length);
        byte[] checksum = SHA256.HashData(data);
        Array.Copy(checksum, 0, dataWithChecksum, data.Length, ChecksumSize);
        return dataWithChecksum;
    }
}
