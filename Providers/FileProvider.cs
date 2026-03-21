using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;
using Comments = System.Collections.Generic.List<NeoIni.Models.Comment>;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Providers;

internal partial class NeoIniFileProvider : INeoIniProvider
{
    public event EventHandler<ProviderErrorEventArgs>? Error;
    public event EventHandler<ChecksumMismatchEventArgs>? ChecksumMismatch;

    internal void DeleteBackup() { if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath); }

    internal void DeleteFile()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
    }

    public NeoIniData GetData(bool humanization = false)
    {
        Data data = new();
        Comments comments = new();
        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return new(data, comments);
        }
        string? currentSection = null;
        var lines = ReadFile();
        if (lines is null) return new(data, comments);
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
            if (currentSection is not null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string? key, out string? value))
                NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
        }
        return new(data, comments);
    }

    public async Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default)
    {
        Data data = new();
        Comments comments = new();
        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return new(data, comments);
        }
        var lines = await ReadFileAsync(ct).ConfigureAwait(false);
        if (lines is null) return new(data, comments);
        string? currentSection = null;
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
            if (currentSection is not null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string? key, out string? value))
                NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
        }
        return new(data, comments);
    }

    public void Save(string content, bool useChecksum)
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
                if (EncryptionKey is null) throw new MissingEncryptionKeyException("The encryption key cannot be null.");
                if (Salt is null) throw new MissingSaltException();
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
            if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, UseBackup ? BackupFilePath : null);
            else File.Move(TempFilePath, FilePath);
        }
        catch (UnauthorizedAccessException ex) { RaiseError(this, new(ex)); }
        catch (IOException ex) { RaiseError(this, new(ex)); }
    }

    public async Task SaveAsync(string content, bool useChecksum, CancellationToken ct)
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
                if (useChecksum) await ms.WriteAsync(WarningBytes, ct).ConfigureAwait(false);
                await ms.WriteAsync(plaintextBytes, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
            }
            else
            {
                if (EncryptionKey is null) throw new MissingEncryptionKeyException("The encryption key cannot be null.");
                if (Salt is null) throw new MissingSaltException();
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                await using MemoryStream ms = new();
                await ms.WriteAsync(header, ct).ConfigureAwait(false);
                if (useChecksum) await ms.WriteAsync(WarningBytes, ct).ConfigureAwait(false);
                await ms.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length), ct).ConfigureAwait(false);
                await ms.WriteAsync(Salt, ct).ConfigureAwait(false);
                using var encryptor = aes.CreateEncryptor();
                await using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    await cs.WriteAsync(plaintextBytes, ct).ConfigureAwait(false);
                    await cs.FlushFinalBlockAsync(ct).ConfigureAwait(false);
                }
                ct.ThrowIfCancellationRequested();
                dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, UseBackup ? BackupFilePath : null);
            else File.Move(TempFilePath, FilePath);
        }
        catch (UnauthorizedAccessException ex) { RaiseError(this, new(ex)); }
        catch (IOException ex) { RaiseError(this, new(ex)); }
    }

    internal static byte[]? GetSalt(string? path)
    {
        if (!File.Exists(path)) return null;
        byte[] fileBytes = NeoIniIO.ReadAllBytes(path);
        if (!TryParseHeader(fileBytes, out var headerParameters)) return null;
        if (headerParameters is null) return null;
        if (!headerParameters.IsEncrypted) return null;
        if (!TryReadSalt(fileBytes, headerParameters.HeaderLength, headerParameters.HasChecksum, out byte[]? salt)) return null;
        return salt;
    }

    public byte[] GetStateChecksum()
    {
        if (!File.Exists(FilePath)) return Array.Empty<byte>();
        var lastWrite = File.GetLastWriteTimeUtc(FilePath);
        var length = new FileInfo(FilePath).Length;
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(lastWrite.Ticks));
        ms.Write(BitConverter.GetBytes(length));
        return ms.ToArray();
    }

    public void RaiseError(object? sender, ProviderErrorEventArgs e)
    {
        if (Error is not null) Error.Invoke(sender, e);
        else throw e.Exception;
    }
}
