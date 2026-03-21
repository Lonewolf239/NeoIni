using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;

namespace NeoIni.Providers;

internal partial class NeoIniFileProvider
{
    private static string[] SplitLines(string content) => content.Split(LineSeparators, StringSplitOptions.None);

    private static byte[] AddChecksum(byte[] data, bool useChecksum)
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

    private string[]? CheckBackup()
    {
        if (!File.Exists(BackupFilePath)) return null;
        return ReadFile(BackupFilePath, true);
    }

    private async Task<string[]?> CheckBackupAsync(CancellationToken ct)
    {
        if (!File.Exists(BackupFilePath)) return null;
        return await ReadFileAsync(BackupFilePath, true, ct).ConfigureAwait(false);
    }

    private EncryptionParameters GetEncryptionParameters(string path, bool autoModeEncryption)
    {
        if (EncryptionKey is null)
        {
            if (autoModeEncryption) return EncryptionProvider.GetEncryptionParameters(salt: GetSalt(path));
            else throw new MissingEncryptionKeyException();
        }
        else
        {
            if (!AutoEncryption && autoModeEncryption)
                return EncryptionProvider.GetEncryptionParameters(salt: GetSalt(path));
            if (Salt is null) throw new MissingSaltException();
            return new EncryptionParameters((byte[])EncryptionKey.Clone(), (byte[])Salt.Clone());
        }
    }

    private bool ValidateFile(byte[]? fileBytes, out HeaderParameters? headerParameters)
    {
        headerParameters = null;
        if (fileBytes is null) return false;
        if (!TryParseHeader(fileBytes, out headerParameters))
            return false;
        if (headerParameters is null) return false;
        int minLength = headerParameters.HeaderLength +
            (headerParameters.HasChecksum ? WarningBytes.Length + ChecksumSize : 0) +
            (headerParameters.IsEncrypted ? IvSize + SaltSize : 0);
        if (fileBytes.Length < minLength) return false;
        if (!ValidateChecksum(fileBytes, headerParameters.HasChecksum))
            return false;
        return true;
    }

    private string[]? ReadError(Exception ex, bool isBackup)
    {
        if (!isBackup)
        {
            var data = CheckBackup();
            if (data is not null) return data;
            RaiseError(this, new(ex));
        }
        return null;
    }

    private string[]? ReadFile() => ReadFile(FilePath, false);

    private string[]? ReadFile(string path, bool isBackup)
    {
        if (!File.Exists(path))
        {
            if (isBackup) return null;
            return CheckBackup();
        }
        try
        {
            byte[] fileBytes = NeoIniIO.ReadAllBytes(path);
            if (!ValidateFile(fileBytes, out var headerParameters))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            if (headerParameters is null) return null;
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
                if (encryptionParameters.Key is null)
                    throw new MissingEncryptionKeyException("The encryption key cannot be null.");
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                byte[] encryptedBytes = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedBytes, 0, encryptedLength);
                byte[] decryptedBytes = EncryptionProvider.Decrypt(encryptionParameters.Key, iv, encryptedBytes);
                content = Encoding.UTF8.GetString(decryptedBytes);
                return SplitLines(content);
            }
        }
        catch (CryptographicException ex) { return ReadError(new InvalidEncryptionKeyException(ex), isBackup); }
        catch (MissingEncryptionKeyException ex) { return ReadError(ex, isBackup); }
        catch (UnauthorizedAccessException ex) { return ReadError(ex, isBackup); }
        catch (IOException ex) { return ReadError(ex, isBackup); }
    }

    private async Task<string[]?> ReadFileAsync(CancellationToken ct) => await ReadFileAsync(FilePath, false, ct).ConfigureAwait(false);

    private async Task<string[]?> ReadFileAsync(string path, bool isBackup, CancellationToken ct)
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
            if (!ValidateFile(fileBytes, out var headerParameters))
            {
                if (isBackup) return null;
                return CheckBackup();
            }
            if (headerParameters is null) return null;
            int index = headerParameters.HeaderLength;
            if (headerParameters.HasChecksum) index += WarningBytes.Length;
            string content;
            if (!headerParameters.IsEncrypted)
            {
                int dataLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (dataLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                content = Encoding.UTF8.GetString(fileBytes, index, dataLength);
                return SplitLines(content);
            }
            else
            {
                var encryptionParameters = GetEncryptionParameters(path, headerParameters.AutoModeEncryption);
                if (encryptionParameters.Key is null)
                    throw new MissingEncryptionKeyException("The encryption key cannot be null.");
                byte[] iv = new byte[IvSize];
                Array.Copy(fileBytes, index, iv, 0, IvSize);
                index += IvSize + SaltSize;
                int encryptedLength = fileBytes.Length - index - (headerParameters.HasChecksum ? ChecksumSize : 0);
                if (encryptedLength <= 0) return null;
                ct.ThrowIfCancellationRequested();
                byte[] encryptedBytes = new byte[encryptedLength];
                Array.Copy(fileBytes, index, encryptedBytes, 0, encryptedLength);
                byte[] decryptedBytes = await EncryptionProvider.DecryptAsync(encryptionParameters.Key, iv, encryptedBytes, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                content = Encoding.UTF8.GetString(decryptedBytes);
                ct.ThrowIfCancellationRequested();
                return SplitLines(content);
            }
        }
        catch (CryptographicException ex) { return ReadError(new InvalidEncryptionKeyException(ex), isBackup); }
        catch (MissingEncryptionKeyException ex) { return ReadError(ex, isBackup); }
        catch (UnauthorizedAccessException ex) { return ReadError(ex, isBackup); }
        catch (IOException ex) { return ReadError(ex, isBackup); }
    }

    private static bool TryReadSalt(byte[] fileBytes, int headerLength, bool hasChecksum, out byte[]? salt)
    {
        int start = headerLength + (hasChecksum ? WarningBytes.Length : 0) + IvSize;
        if (fileBytes.Length < start + SaltSize) { salt = null; return false; }
        salt = fileBytes[start..(start + SaltSize)];
        return true;
    }

    private static bool TryParseHeader(byte[]? fileBytes, out HeaderParameters? headerParameters)
    {
        headerParameters = null;
        if (fileBytes is null) return false;
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
}
