using System;
using System.Collections.Generic;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Internal;

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

internal sealed class NeoIniData
{
    internal Data Data { get; }
    internal List<Comment> Comments { get; }

    internal NeoIniData(Data data, List<Comment> comments) { Data = data; Comments = comments; }
}

internal enum CommentType { FreeSpace, Up, Right, Down }

internal sealed class Comment
{
    internal string Line { get; }
    internal CommentType CommentType { get; }
    internal string Content { get; }

    internal Comment(string line, CommentType commentType, string content)
    {
        Line = line;
        CommentType = commentType;
        Content = content;
    }
}
