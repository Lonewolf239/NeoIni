using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni.Core;

internal sealed class NeoIniIO
{
    internal static void WriteBytes(string path, byte[] data) => File.WriteAllBytes(path, data);

    internal static byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    internal static async Task WriteBytesAsync(string path, byte[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.Asynchronous, preallocationSize: data.Length);
        ct.ThrowIfCancellationRequested();
        await RandomAccess.WriteAsync(handle, data, fileOffset: 0, ct).ConfigureAwait(false);
    }

    internal static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
        long length = RandomAccess.GetLength(handle);
        if (length > int.MaxValue) throw new IOException("File is too large.");
        byte[] result = new byte[length];
        ct.ThrowIfCancellationRequested();
        await RandomAccess.ReadAsync(handle, result, fileOffset: 0, ct).ConfigureAwait(false);
        return result;
    }
}
