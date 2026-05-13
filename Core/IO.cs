using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni.Core
{
    internal sealed class NeoIniIO
    {
        internal static byte[] ReadBytes(string path, int length, int startIndex = 0)
        {
#if NET6_0_OR_GREATER
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            long fileLength = RandomAccess.GetLength(handle);
            if (fileLength < startIndex + length) throw new IOException("File too small to read the requested range.");
            byte[] buffer = new byte[length];
            int bytesRead = RandomAccess.Read(handle, buffer, fileOffset: startIndex);
            if (bytesRead != length) throw new IOException("Could not read the requested number of bytes.");
            return buffer;
#else

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
            if (fs.Length < startIndex + length) throw new IOException("File too small to read the requested range.");
            fs.Seek(startIndex, SeekOrigin.Begin);
            byte[] buffer = new byte[length];
            int offset = 0;
            int remaining = length;
            while (remaining > 0)
            {
                int read = fs.Read(buffer, offset, remaining);
                if (read == 0) throw new IOException("Unexpected end of stream.");
                offset += read;
                remaining -= read;
            }
            return buffer;
#endif
        }

        internal static void WriteBytes(string path, byte[] data) => File.WriteAllBytes(path, data);

        internal static byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        internal static async Task WriteBytesAsync(string path, byte[] data, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
            using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.Asynchronous, preallocationSize: data.Length);
            ct.ThrowIfCancellationRequested();
            await RandomAccess.WriteAsync(handle, data, fileOffset: 0, ct).ConfigureAwait(false);
#else
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                await fs.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
#endif
        }

        internal static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            long length = RandomAccess.GetLength(handle);
            if (length > int.MaxValue) throw new IOException("File is too large.");
            byte[] result = new byte[length];
            ct.ThrowIfCancellationRequested();
            await RandomAccess.ReadAsync(handle, result, fileOffset: 0, ct).ConfigureAwait(false);
            return result;
#else
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                long length = fs.Length;
                if (length > int.MaxValue) throw new IOException("File is too large.");
                byte[] result = new byte[length];
                int offset = 0;
                int remaining = (int)length;
                while (remaining > 0)
                {
                    int read = await fs.ReadAsync(result, offset, remaining, ct).ConfigureAwait(false);
                    if (read == 0) break;
                    offset += read;
                    remaining -= read;
                }
                return result;
            }
#endif
        }
    }
}
