using System;

namespace NeoIni;

/// <summary>Provides data for the event that is raised when a file checksum mismatch occurs.</summary>
public sealed class ChecksumMismatchEventArgs : EventArgs
{
    /// <summary>Gets the expected checksum value.</summary>
    public byte[] Expected { get; }

    /// <summary>Gets the actual checksum value that was calculated.</summary>
    public byte[] Actual { get; }

    /// <summary>Initializes a new instance of the <see cref="ChecksumMismatchEventArgs"/> class.</summary>
    /// <param name="expected">The expected checksum array.</param>
    /// <param name="actual">The actual calculated checksum array.</param>
    public ChecksumMismatchEventArgs(byte[] expected, byte[] actual)
    {
        Expected = expected;
        Actual = actual;
    }
}

/// <summary>Provides data for the event that is raised when an error or exception occurs.</summary>
public sealed class ErrorEventArgs : EventArgs
{
    /// <summary>Gets the exception that caused the error.</summary>
    public Exception Exception { get; }

    /// <summary>Initializes a new instance of the <see cref="ErrorEventArgs"/> class.</summary>
    /// <param name="exception">The exception containing information about the error.</param>
    public ErrorEventArgs(Exception exception) => Exception = exception;
}

/// <summary>Provides data for events related to a specific key and its value within a section.</summary>
public sealed class KeyEventArgs : EventArgs
{
    /// <summary>Gets the name of the section containing the key.</summary>
    public string Section { get; }

    /// <summary>Gets the name of the key.</summary>
    public string Key { get; }

    /// <summary>Gets the value assigned to the key.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="KeyEventArgs"/> class.</summary>
    /// <param name="section">The name of the section.</param>
    /// <param name="key">The name of the key.</param>
    /// <param name="value">The value of the key.</param>
    public KeyEventArgs(string section, string key, string value)
    {
        Section = section;
        Key = key;
        Value = value;
    }
}

/// <summary>Provides data for the event that is raised when a key is renamed.</summary>
public sealed class KeyRenamedEventArgs : EventArgs
{
    /// <summary>Gets the name of the section containing the renamed key.</summary>
    public string Section { get; }

    /// <summary>Gets the original name of the key before it was renamed.</summary>
    public string OldName { get; }

    /// <summary>Gets the new name assigned to the key.</summary>
    public string NewName { get; }

    /// <summary>Initializes a new instance of the <see cref="KeyRenamedEventArgs"/> class.</summary>
    /// <param name="section">The name of the section.</param>
    /// <param name="oldName">The original name of the key.</param>
    /// <param name="newName">The new name of the key.</param>
    public KeyRenamedEventArgs(string section, string oldName, string newName)
    {
        Section = section;
        OldName = oldName;
        NewName = newName;
    }
}

/// <summary>Provides data for the event that is raised when a key is removed.</summary>
public sealed class KeyRemovedEventArgs : EventArgs
{
    /// <summary>Gets the name of the section from which the key was removed.</summary>
    public string Section { get; }

    /// <summary>Gets the name of the removed key.</summary>
    public string Key { get; }

    /// <summary>Initializes a new instance of the <see cref="KeyRemovedEventArgs"/> class.</summary>
    /// <param name="section">The name of the section.</param>
    /// <param name="key">The name of the removed key.</param>
    public KeyRemovedEventArgs(string section, string key)
    {
        Section = section;
        Key = key;
    }
}

/// <summary>Provides data for events related to an entire section.</summary>
public sealed class SectionEventArgs : EventArgs
{
    /// <summary>Gets the name of the section.</summary>
    public string Section { get; }

    /// <summary>Initializes a new instance of the <see cref="SectionEventArgs"/> class.</summary>
    /// <param name="section">The name of the section.</param>
    public SectionEventArgs(string section) => Section = section;
}

/// <summary>Provides data for the event that is raised when a section is renamed.</summary>
public sealed class SectionRenamedEventArgs : EventArgs
{
    /// <summary>Gets the original name of the section before it was renamed.</summary>
    public string OldSection { get; }

    /// <summary>Gets the new name assigned to the section.</summary>
    public string NewSection { get; }

    /// <summary>Initializes a new instance of the <see cref="SectionRenamedEventArgs"/> class.</summary>
    /// <param name="oldSection">The original name of the section.</param>
    /// <param name="newSection">The new name of the section.</param>
    public SectionRenamedEventArgs(string oldSection, string newSection)
    {
        OldSection = oldSection;
        NewSection = newSection;
    }
}

/// <summary>Provides data for the event that is raised after a search operation completes.</summary>
public sealed class SearchCompletedEventArgs : EventArgs
{
    /// <summary>Gets the pattern that was used for the search.</summary>
    public string SearchPattern { get; }

    /// <summary>Gets the total number of matches found during the search.</summary>
    public int MatchesCount { get; }

    /// <summary>Initializes a new instance of the <see cref="SearchCompletedEventArgs"/> class.</summary>
    /// <param name="searchPattern">The pattern used for the search.</param>
    /// <param name="matchesCount">The number of successful matches found.</param>
    public SearchCompletedEventArgs(string searchPattern, int matchesCount)
    {
        SearchPattern = searchPattern;
        MatchesCount = matchesCount;
    }
}
