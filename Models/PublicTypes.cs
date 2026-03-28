using System;
using System.Collections.Generic;
using System.Globalization;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Models
{
#if NETSTANDARD2_0
    /// <summary>Represents encryption parameters consisting of a key and a salt.</summary>
    public class EncryptionParameters
    {
        /// <summary>Gets the encryption key as a byte array. May be null if not applicable.</summary>
        public byte[]? Key { get; }

        /// <summary>Gets the salt used in key derivation as a byte array. May be null if not applicable.</summary>
        public byte[]? Salt { get; }

        /// <summary>Initializes a new instance of the <see cref="EncryptionParameters"/> class.</summary>
        /// <param name="key">The encryption key as a byte array. May be null if not applicable.</param>
        /// <param name="salt">The salt used in key derivation as a byte array. May be null if not applicable.</param>
        public EncryptionParameters(byte[]? key, byte[]? salt)
        {
            Key = key;
            Salt = salt;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj) => obj is EncryptionParameters other && Equals(Key, other.Key) && Equals(Salt, other.Salt);

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Key != null ? Key.GetHashCode() : 0);
                hash = hash * 23 + (Salt != null ? Salt.GetHashCode() : 0);
                return hash;
            }
        }
    }
#else
    /// <summary>Represents encryption parameters consisting of a key and a salt.</summary>
    /// <param name="Key">The encryption key as a byte array. May be null if not applicable.</param>
    /// <param name="Salt">The salt used in key derivation as a byte array. May be null if not applicable.</param>
    public record EncryptionParameters(byte[]? Key, byte[]? Salt);
#endif

#if NETSTANDARD2_0
    /// <summary>Represents a matched entry found during a search operation in the INI file.</summary>
    public class SearchResult
    {
        /// <summary>Gets the name of the section where the match was found.</summary>
        public string Section { get; }

        /// <summary>Gets the key of the matched entry.</summary>
        public string Key { get; }

        /// <summary>Gets the value of the matched entry.</summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of the <see cref="SearchResult"/> class.</summary>
        /// <param name="section">The name of the section where the match was found.</param>
        /// <param name="key">The key of the matched entry.</param>
        /// <param name="value">The value of the matched entry.</param>
        public SearchResult(string section, string key, string value)
        {
            Section = section;
            Key = key;
            Value = value;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj) => obj is SearchResult other && Section == other.Section && Key == other.Key && Value == other.Value;

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Section != null ? Section.GetHashCode() : 0);
                hash = hash * 23 + (Key != null ? Key.GetHashCode() : 0);
                hash = hash * 23 + (Value != null ? Value.GetHashCode() : 0);
                return hash;
            }
        }
    }
#else
    /// <summary>Represents a matched entry found during a search operation in the INI file.</summary>
    /// <param name="Section">The name of the section where the match was found.</param>
    /// <param name="Key">The key of the matched entry.</param>
    /// <param name="Value">The value of the matched entry.</param>
    public record SearchResult(string Section, string Key, string Value);
#endif

    /// <summary>Represents the parsed data and comments extracted from an INI configuration source.</summary>
    public class NeoIniData
    {
        /// <summary>Gets the structured dictionary containing sections, keys, and their corresponding values.</summary>
        public Data? Data { get; }

        /// <summary>Gets the collection of comments preserved from the INI source.</summary>
        public List<Comment>? Comments { get; }

        /// <summary>Initializes a new instance of the <see cref="NeoIniData"/> class.</summary>
        /// <param name="data">The parsed INI data structure.</param>
        /// <param name="comments">The list of parsed INI comments.</param>
        public NeoIniData(Data? data, List<Comment>? comments)
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
        public string? Line { get; }

        /// <summary>Gets the positional type of the comment indicating where it was placed in the original source.</summary>
        public CommentType CommentType { get; }

        /// <summary>Gets the actual text content of the comment.</summary>
        public string Content { get; }

        /// <summary>Initializes a new instance of the <see cref="Comment"/> class.</summary>
        /// <param name="line">The associated line or element identifier.</param>
        /// <param name="commentType">The positional type of the comment.</param>
        /// <param name="content">The text content of the comment.</param>
        public Comment(string? line, CommentType commentType, string content)
        {
            Line = line;
            CommentType = commentType;
            Content = content;
        }
    }

    /// <summary>
    /// Represents a key-value pair used for bulk operations on an INI document.
    /// This class encapsulates a section, key, and a value that will be stored as a string.
    /// </summary>
    /// <remarks>
    /// When constructing a <see cref="NeoIniValue"/>, if the provided value implements <see cref="IFormattable"/>,
    /// its string representation is obtained using the invariant culture to ensure consistent formatting.
    /// </remarks>
    public class NeoIniValue
    {
        /// <summary>Gets the name of the section where the key-value pair belongs.</summary>
        public string Section { get; }

        /// <summary>Gets the key name of the key-value pair.</summary>
        public string Key { get; }

        /// <summary>
        /// Gets the string representation of the value.
        /// This value is derived from the object passed to the constructor, formatted using invariant culture if applicable.
        /// </summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of the <see cref="NeoIniValue"/> class.</summary>
        /// <param name="section">The name of the section for the key-value pair.</param>
        /// <param name="key">The key name.</param>
        /// <param name="value">
        /// The value to be stored. If the value implements <see cref="IFormattable"/>, it is converted to a string using
        /// <see cref="CultureInfo.InvariantCulture"/>; otherwise, <see cref="object.ToString"/> is used.
        /// If <paramref name="value"/> is <c>null</c>, an empty string is stored.
        /// </param>
        public NeoIniValue(string? section, string? key, object? value)
        {
            if (section is null || key is null) throw new ArgumentNullException();
            Section = section;
            Key = key;
            if (value is IFormattable formattable) Value = formattable.ToString(null, CultureInfo.InvariantCulture);
            else Value = value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>Specifies the encryption mode for a NeoIni document.</summary>
    /// <remarks>
    /// The encryption type determines how encryption keys and parameters are managed
    /// when writing encrypted INI files.
    /// </remarks>
    public enum EncryptionType
    {
        /// <summary>No encryption is applied. The INI file is stored in plain text.</summary>
        None,

        /// <summary>Encryption is applied with automatically generated parameters.</summary>
        Auto,

        /// <summary>
        /// Custom encryption using a user-provided password.
        /// Use this mode when you need to specify a custom encryption password
        /// for enhanced security or cross-platform compatibility.
        /// </summary>
        /// <remarks>This mode requires using the constructor that accepts an <c>encryptionPassword</c> parameter.</remarks>
        Custom
    }
}
