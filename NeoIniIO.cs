using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni;

internal sealed class NeoIniIO
{
    private const int ChunkSize = 1024 * 1024;

    internal static void WriteBytes(string path, byte[] data)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, false);
        int offset = 0;
        while (offset < data.Length)
        {
            int count = Math.Min(ChunkSize, data.Length - offset);
            fs.Write(data, offset, count);
            offset += count;
        }
        fs.Flush(true);
    }

    internal static async Task WriteBytesAsync(string path, byte[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        int offset = 0;
        while (offset < data.Length)
        {
            ct.ThrowIfCancellationRequested();
            int count = Math.Min(ChunkSize, data.Length - offset);
            await fs.WriteAsync(data, offset, count, ct).ConfigureAwait(false);
            offset += count;
        }
        await fs.FlushAsync(ct).ConfigureAwait(false);
    }

    internal static byte[] ReadAllBytes(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, false);
        long length = fs.Length;
        if (length > int.MaxValue) throw new IOException("File is too large.");
        byte[] result = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = fs.Read(result, offset, (int)Math.Min(ChunkSize, length - offset));
            if (read == 0) break;
            offset += read;
        }
        if (offset != length) Array.Resize(ref result, offset);
        return result;
    }

    internal static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        long length = fs.Length;
        if (length > int.MaxValue) throw new IOException("File is too large.");
        byte[] result = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            ct.ThrowIfCancellationRequested();
            int read = await fs.ReadAsync(result, offset, (int)Math.Min(ChunkSize, length - offset), ct).ConfigureAwait(false);
            if (read == 0) break;
            offset += read;
        }
        if (offset != length) Array.Resize(ref result, offset);
        return result;
    }
}
