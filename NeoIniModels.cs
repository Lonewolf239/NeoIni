using System;

namespace NeoIni;

/// <summary>
/// Represents a matched entry found during a search operation in the INI file.
/// </summary>
/// <param name="Section">The name of the section where the match was found.</param>
/// <param name="Key">The key of the matched entry.</param>
/// <param name="Value">The value of the matched entry.</param>
public record SearchResult(string Section, string Key, string Value);

internal record EncryptionParameters(byte[] Key, byte[] Salt);

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
    internal bool HasChecksum;
    internal bool IsEncrypted;
    internal bool AutoModeEncryption;

    internal HeaderParameters(HeaderFlags flags)
    {
        HasChecksum = flags.HasFlag(HeaderFlags.HasChecksum);
        IsEncrypted = flags.HasFlag(HeaderFlags.IsEncrypted);
        AutoModeEncryption = flags.HasFlag(HeaderFlags.AutoMode);
    }
}
