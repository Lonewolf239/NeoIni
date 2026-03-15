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

/// <summary>Represents the parsed data and comments extracted from an INI configuration source.</summary>
public class NeoIniData
{
    /// <summary>Gets the structured dictionary containing sections, keys, and their corresponding values.</summary>
    public Data Data { get; }

    /// <summary>Gets the collection of comments preserved from the INI source.</summary>
    public List<Comment> Comments { get; }

    /// <summary>Initializes a new instance of the <see cref="NeoIniData"/> class.</summary>
    /// <param name="data">The parsed INI data structure.</param>
    /// <param name="comments">The list of parsed INI comments.</param>
    public NeoIniData(Data data, List<Comment> comments)
    {
        Data = data;
        Comments = comments;
    }
}

/// <summary>Specifies the positional relationship of a comment relative to INI configuration elements.</summary>
public enum CommentType
{
    /// <summary>
    /// A standalone comment that is not explicitly attached to any specific key or section (e.g., empty lines or floating comments).
    /// </summary>
    FreeSpace,

    /// <summary>
    /// A comment placed directly above a section header or a key-value pair.
    /// </summary>
    Up,

    /// <summary>
    /// An inline comment placed to the right of a section header or a key-value pair on the same line.
    /// </summary>
    Right,

    /// <summary>
    /// A comment placed directly below a section header or a key-value pair.
    /// </summary>
    Down
}

/// <summary>Represents a single comment within an INI configuration, including its content and position.</summary>
public class Comment
{
    /// <summary>Gets the associated identifier or line string this comment is bound to.</summary>
    public string Line { get; }

    /// <summary>Gets the positional type of the comment indicating where it was placed in the original source.</summary>
    public CommentType CommentType { get; }

    /// <summary>Gets the actual text content of the comment.</summary>
    public string Content { get; }

    /// <summary>Initializes a new instance of the <see cref="Comment"/> class.</summary>
    /// <param name="line">The associated line or element identifier.</param>
    /// <param name="commentType">The positional type of the comment.</param>
    /// <param name="content">The text content of the comment.</param>
    public Comment(string line, CommentType commentType, string content)
    {
        Line = line;
        CommentType = commentType;
        Content = content;
    }
}
