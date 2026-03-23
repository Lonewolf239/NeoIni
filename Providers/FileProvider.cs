using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;
using Comments = System.Collections.Generic.List<NeoIni.Models.Comment>;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Providers
{
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
            var data = new Data();
            var comments = new Comments();
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(FilePath))
            {
                using var stream = File.Create(FilePath);
                return new NeoIniData(data, comments);
            }
            string? currentSection = null;
            var lines = ReadFile();
            if (lines is null) return new NeoIniData(data, comments);
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
#if NETSTANDARD2_0
                if (!(currentSection is null) && NeoIniParser.TryMatchKey(trimmed, out string? key, out string? value))
#else
                if (currentSection is not null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string? key, out string? value))
#endif
                    NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
            }
            return new NeoIniData(data, comments);
        }

        public async Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default)
        {
            var data = new Data();
            var comments = new Comments();
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(FilePath))
            {
                using var stream = File.Create(FilePath);
                return new NeoIniData(data, comments);
            }
            var lines = await ReadFileAsync(ct).ConfigureAwait(false);
            if (lines is null) return new NeoIniData(data, comments);
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
#if NETSTANDARD2_0
                if (!(currentSection is null) && NeoIniParser.TryMatchKey(trimmed, out string? key, out string? value))
#else
                if (currentSection is not null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string? key, out string? value))
#endif
                    NeoIniParser.HandleKeyValueLine(trimmed, currentSection, key, value, humanization, data, comments);
            }
            return new NeoIniData(data, comments);
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
                    using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
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
                    using var ms = new MemoryStream();
                    ms.Write(header, 0, header.Length);
                    if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
                    EncryptionProvider.Encrypt(ms, EncryptionKey, Salt, plaintextBytes);
                    dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                    NeoIniIO.WriteBytes(TempFilePath, dataWithChecksum);
                }
                if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, UseBackup ? BackupFilePath : null);
                else File.Move(TempFilePath, FilePath);
            }
            catch (UnauthorizedAccessException ex) { RaiseError(this, new ProviderErrorEventArgs(ex)); }
            catch (IOException ex) { RaiseError(this, new ProviderErrorEventArgs(ex)); }
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
                    using var ms = new MemoryStream(plaintextBytes.Length + (useChecksum ? WarningBytes.Length : 0));
                    ms.Write(header, 0, header.Length);
#if NETSTANDARD2_0
                    if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
                    ms.Write(plaintextBytes, 0, plaintextBytes.Length);
#else
					if (useChecksum) await ms.WriteAsync(WarningBytes, ct).ConfigureAwait(false);
					await ms.WriteAsync(plaintextBytes, ct).ConfigureAwait(false);
#endif
                    ct.ThrowIfCancellationRequested();
                    dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                    await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
                }
                else
                {
                    if (EncryptionKey is null) throw new MissingEncryptionKeyException("The encryption key cannot be null.");
                    if (Salt is null) throw new MissingSaltException();
#if NETSTANDARD2_0
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(header, 0, header.Length);
                        if (useChecksum) ms.Write(WarningBytes, 0, WarningBytes.Length);
#else
						await using MemoryStream ms = new();
						await ms.WriteAsync(header, ct).ConfigureAwait(false);
						if (useChecksum) await ms.WriteAsync(WarningBytes, ct).ConfigureAwait(false);
#endif
                        await EncryptionProvider.EncryptAsync(ms, EncryptionKey, Salt, plaintextBytes, ct).ConfigureAwait(false);
                        ct.ThrowIfCancellationRequested();
                        dataWithChecksum = AddChecksum(ms.ToArray(), useChecksum);
                        await NeoIniIO.WriteBytesAsync(TempFilePath, dataWithChecksum, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
                    }
#endif
                }
                ct.ThrowIfCancellationRequested();
                if (File.Exists(FilePath)) File.Replace(TempFilePath, FilePath, UseBackup ? BackupFilePath : null);
                else File.Move(TempFilePath, FilePath);
            }
            catch (UnauthorizedAccessException ex) { RaiseError(this, new ProviderErrorEventArgs(ex)); }
            catch (IOException ex) { RaiseError(this, new ProviderErrorEventArgs(ex)); }
        }

        internal static byte[]? GetSalt(string? path)
        {
            if (path is null) return null;
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
#if NETSTANDARD2_0
#else
            ms.Write(BitConverter.GetBytes(lastWrite.Ticks));
            ms.Write(BitConverter.GetBytes(length));
#endif
            return ms.ToArray();
        }

        public void RaiseError(object? sender, ProviderErrorEventArgs e)
        {
#if NETSTANDARD2_0
            if (!(Error is null)) Error.Invoke(sender, e);
#else

            if (Error is not null) Error.Invoke(sender, e);
#endif
            else throw e.Exception;
        }
    }
}
