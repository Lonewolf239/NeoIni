using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NeoIni.Models;
using Comments = System.Collections.Generic.List<NeoIni.Models.Comment>;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Core;

internal partial class NeoIniParser
{
    internal static string FormatInvariant<T>(T value) =>
        value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? string.Empty;

    internal static string ValueToString<T>(T value)
    {
        var s = FormatInvariant(value);
        if (s.Length == 0) return s;
        StringBuilder sb = new(s.Length * 2);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\r')
            {
                if (i + 1 < s.Length && s[i + 1] == '\n')
                {
                    sb.Append(@"\r\n");
                    i++;
                }
                else sb.Append(@"\r");
                continue;
            }
            if (c == '\n') { sb.Append(@"\n"); continue; }
            if (c == '\\') { sb.Append(@"\\"); continue; }
            sb.Append(c);
        }
        return FormatInvariant(sb);
    }

    internal static string GetStringRaw(string raw) => Unescape(raw);

    internal static string GetStringRaw(Data data, string section, string keyName)
    {
        string raw = data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val.Trim() : null;
        return Unescape(raw);
    }

    internal static T TryParseValue<T>(string value, T defaultValue, EventHandler<ProviderErrorEventArgs> onError)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        try
        {
            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out object enumResult) ? (T)enumResult : defaultValue;
            if (targetType == typeof(bool))
                return bool.TryParse(value, out bool boolResult) ? (T)(object)boolResult : defaultValue;
            if (targetType == typeof(DateTime))
                return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dtResult) ?
                     (T)(object)dtResult : defaultValue;
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            onError?.Invoke(null, new(ex));
            return defaultValue;
        }
    }

    internal static bool TryMatchKey(ReadOnlySpan<char> line, out string key, out string value)
    {
        key = null;
        value = null;
        int eqIndex = line.IndexOf('=');
        if (eqIndex == -1) return false;
        ReadOnlySpan<char> keySpan = line[..eqIndex].Trim();
        if (keySpan.IsEmpty) return false;
        ReadOnlySpan<char> valueSpan;
        int quoteIndex = line.IndexOf('"');
        if (quoteIndex == -1)
        {
            int semicolonIndex = line.IndexOf(';');
            if (semicolonIndex > eqIndex) valueSpan = line[(eqIndex + 1)..semicolonIndex].Trim();
            else valueSpan = line[(eqIndex + 1)..].Trim();
        }
        else valueSpan = ParseShielding(line);
        key = keySpan.ToString();
        value = valueSpan.ToString();
        return true;
    }

    internal static T Clamp<T>(T value, T minValue, T maxValue) where T : IComparable<T>
    {
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(minValue, maxValue) > 0)
            throw new ArgumentException($"'{nameof(minValue)}' cannot be greater than '{nameof(maxValue)}'.");
        if (comparer.Compare(value, minValue) < 0) return minValue;
        if (comparer.Compare(value, maxValue) > 0) return maxValue;
        return value;
    }

    internal static string GetContent(Data data, Comments commentsData, bool humanization, bool useShielding)
    {
        if (data == null || data.Count == 0) return string.Empty;
        var estimatedSize = Environment.NewLine.Length;
        Comments comments = new(commentsData);
        foreach (var section in data)
        {
            estimatedSize += section.Key.Length + 2 + Environment.NewLine.Length;
            foreach (var kvp in section.Value)
                estimatedSize += kvp.Key.Length + (kvp.Value?.Length ?? 0) + 3 + (useShielding ? 2 : 0) + Environment.NewLine.Length;
            estimatedSize += Environment.NewLine.Length;
        }
        if (comments != null && comments.Count > 0)
        {
            foreach (var comment in comments)
                estimatedSize += (comment.Content?.Length ?? 0) + 3 + Environment.NewLine.Length;
        }
        var content = new StringBuilder(estimatedSize);
        content.AppendLine();
        foreach (var section in data)
        {
            var sectionLine = $"[{section.Key}]";
            content.AppendLine(GetContentHelper(section.Key, sectionLine, comments));
            foreach (var kvp in section.Value)
            {
                string keyValueLine = $"{kvp.Key} = " +
                    $"{(useShielding ? '"' : string.Empty) + kvp.Value + (useShielding ? '"' : string.Empty)}";
                content.AppendLine(GetContentHelper(kvp.Key, keyValueLine, comments));
            }
            content.AppendLine();
        }
        if (comments != null && comments.Count > 0)
        {
            var remainingComments = comments.Where(c => c.CommentType == CommentType.FreeSpace &&
                    !string.IsNullOrEmpty(c.Content)).Select(c => $"; {c.Content}");
            var prefix = new StringBuilder();
            foreach (var line in remainingComments) prefix.AppendLine(line);
            if (prefix.Length > 0) content.Insert(0, prefix.ToString());
        }
        return content.ToString();
    }

    internal static bool IsCommentLine(string trimmed) => !string.IsNullOrEmpty(trimmed) && trimmed[0] == ';';

    internal static bool IsSectionLine(string trimmed)
    {
        if (string.IsNullOrEmpty(trimmed)) return false;
        TryParseLine(trimmed, out var section, out _);
        return section.Length > 1 && section[0] == '[' && section[^1] == ']';
    }

    internal static void HandleCommentLine(string[] lines, int index, string trimmed, bool humanization, Comments comments)
    {
        if (!humanization) return;
        if (comments == null) return;
        if (string.IsNullOrEmpty(trimmed)) return;
        string nearestString = string.Empty;
        var commentType = CommentType.FreeSpace;
        if (index + 1 < lines.Length)
        {
            string line = lines[index + 1];
            if (!string.IsNullOrEmpty(line) && !line.StartsWith(';'))
            {
                commentType = CommentType.Up;
                nearestString = ParseNearestLine(line);
            }
        }
        if (index > 1)
        {
            string line = lines[index - 1];
            if (!string.IsNullOrEmpty(line) && !line.StartsWith(';'))
            {
                commentType = CommentType.Down;
                nearestString = ParseNearestLine(line);
            }
        }
        var commentText = trimmed.TrimStart(';').Trim();
        if (commentText.Length == 0) return;
        comments.Add(new Comment(nearestString, commentType, commentText));
    }

    internal static string HandleSectionLine(string trimmed, bool humanization, Data data, Comments comments)
    {
        TryParseLine(trimmed, out var sectionPart, out var comment);
        var section = sectionPart.Trim('[', ']');
        if (!data.TryGetValue(section, out var dict)) data[section] = new();
        if (humanization && !string.IsNullOrEmpty(comment) && comments != null)
            comments.Add(new Comment(section, CommentType.Right, comment));
        return section;
    }

    internal static void HandleKeyValueLine(string trimmed, string currentSection, string key, string value, bool humanization, Data data, Comments comments)
    {
        data[currentSection][key] = value;
        if (!humanization || comments == null) return;
        if (!TryParseLine(trimmed, out _, out var afterSemicolon)) return;
        if (!string.IsNullOrEmpty(afterSemicolon)) comments.Add(new Comment(key, CommentType.Right, afterSemicolon));
    }
}
