using System;

namespace NeoIni.Models;

internal record EncryptionParameters(byte[]? Key, byte[]? Salt);

[Flags]
internal enum HeaderFlags : byte
{
    None = 0,
    HasChecksum = 1 << 0,
    IsEncrypted = 1 << 1,
    AutoMode = 1 << 2,
    CustomMode = 1 << 3
}

internal sealed class HeaderParameters
{
    internal int HeaderLength;
    internal bool HasChecksum { get; }
    internal bool IsEncrypted { get; }
    internal bool AutoModeEncryption { get; }

    internal HeaderParameters(HeaderFlags flags)
    {
        HasChecksum = flags.HasFlag(HeaderFlags.HasChecksum);
        IsEncrypted = flags.HasFlag(HeaderFlags.IsEncrypted);
        AutoModeEncryption = flags.HasFlag(HeaderFlags.AutoMode);
    }
}
