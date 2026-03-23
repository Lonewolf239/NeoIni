using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NeoIni.Models;
using Comments = System.Collections.Generic.List<NeoIni.Models.Comment>;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Core
{
    internal partial class NeoIniParser
    {
#if NETSTANDARD2_0
        internal static string FormatInvariant<T>(T value) =>
#else
        internal static string FormatInvariant<T>(T? value) =>
#endif
            value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? string.Empty;

#if NETSTANDARD2_0
        internal static string ValueToString<T>(T value)
#else
        internal static string ValueToString<T>(T? value)
#endif
        {
            var s = FormatInvariant(value);
            if (s.Length == 0) return s;
            var sb = new StringBuilder(s.Length * 2);
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

        internal static string? GetStringRaw(string? raw) => Unescape(raw);


        internal static string? GetStringRaw(Data? data, string section, string keyName)
        {
            string? raw = null;
#if NETSTANDARD2_0
            if (!(data is null) && data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val))
#else
            if (data is not null && data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val))
#endif
                raw = val.Trim();
            return Unescape(raw);
        }

#if NETSTANDARD2_0
        internal static T TryParseValue<T>(string? value, T defaultValue, EventHandler<ProviderErrorEventArgs>? onError)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            try
            {
                if (targetType.IsEnum)
                {
                    try { return (T)Enum.Parse(targetType, value, true); }
                    catch { return defaultValue; }
                }
                if (targetType == typeof(bool))
                    return bool.TryParse(value, out bool boolResult) ? (T)(object)boolResult : defaultValue;
                if (targetType == typeof(DateTime))
                    return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dtResult) ?
                         (T)(object)dtResult : defaultValue;
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                onError?.Invoke(null, new ProviderErrorEventArgs(ex));
                return defaultValue;
            }
            catch (InvalidCastException ex)
            {
                onError?.Invoke(null, new ProviderErrorEventArgs(ex));
                return defaultValue;
            }
        }
#else
        internal static T? TryParseValue<T>(string? value, T? defaultValue, EventHandler<ProviderErrorEventArgs>? onError)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            try
            {
                if (targetType.IsEnum)
                    return Enum.TryParse(targetType, value, true, out object? enumResult) && enumResult is not null ? (T)enumResult : defaultValue;
                if (targetType == typeof(bool))
                    return bool.TryParse(value, out bool boolResult) ? (T)(object)boolResult : defaultValue;
                if (targetType == typeof(DateTime))
                    return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dtResult) ?
                         (T)(object)dtResult : defaultValue;
                return (T?)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                onError?.Invoke(null, new ProviderErrorEventArgs(ex));
                return defaultValue;
            }
            catch (InvalidCastException ex)
            {
                onError?.Invoke(null, new ProviderErrorEventArgs(ex));
                return defaultValue;
            }
        }
#endif

#if NETSTANDARD2_0
        internal static bool TryMatchKey(string line, out string? key, out string? value)
        {
            key = null;
            value = null;
            int eqIndex = line.IndexOf('=');
            if (eqIndex == -1) return false;
            string keyStr = line.Substring(0, eqIndex).Trim();
            if (string.IsNullOrEmpty(keyStr)) return false;
            string valueStr;
            int quoteIndex = line.IndexOf('"');
            if (quoteIndex == -1)
            {
                int semicolonIndex = line.IndexOf(';');
                if (semicolonIndex > eqIndex)
                    valueStr = line.Substring(eqIndex + 1, semicolonIndex - eqIndex - 1).Trim();
                else
                    valueStr = line.Substring(eqIndex + 1).Trim();
            }
            else
                valueStr = ParseShielding(line);
            key = keyStr;
            value = valueStr;
            return true;
        }
#else
        internal static bool TryMatchKey(ReadOnlySpan<char> line, out string? key, out string? value)
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
#endif

#if NETSTANDARD2_0
        internal static T Clamp<T>(T value, T minValue, T maxValue) where T : IComparable<T>
#else
        internal static T Clamp<T>(T? value, T minValue, T maxValue) where T : IComparable<T>
#endif
        {
            var comparer = Comparer<T>.Default;
            if (comparer.Compare(minValue, maxValue) > 0)
                throw new ArgumentException($"'{nameof(minValue)}' cannot be greater than '{nameof(maxValue)}'.");
            if (value is null) return minValue;
            if (comparer.Compare(value, minValue) < 0) return minValue;
            if (comparer.Compare(value, maxValue) > 0) return maxValue;
            return value;
        }

        internal static string GetContent(Data? data, Comments? commentsData, bool useShielding)
        {
            if (data is null || data.Count == 0) return string.Empty;
            int estimatedSize = Environment.NewLine.Length;
#if NETSTANDARD2_0
            Comments comments = !(commentsData is null) ? new Comments(commentsData) : new Comments();
#else
            Comments comments = commentsData is not null ? new(commentsData) : new();
#endif
            foreach (var section in data)
            {
                estimatedSize += section.Key.Length + 2 + Environment.NewLine.Length;
                foreach (var kvp in section.Value)
                    estimatedSize += kvp.Key.Length + (kvp.Value?.Length ?? 0) + 3 + (useShielding ? 2 : 0) + Environment.NewLine.Length;
                estimatedSize += Environment.NewLine.Length;
            }
            if (comments.Count > 0)
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
#if NETSTANDARD2_0
                    string keyValueLine = string.Format("{0} = {1}{2}{1}", kvp.Key, useShielding ? "\"" : string.Empty, kvp.Value);
#else
					string keyValueLine = $"{kvp.Key} = " +
						$"{(useShielding ? '"' : string.Empty)}{kvp.Value}{(useShielding ? '"' : string.Empty)}";
#endif
                    content.AppendLine(GetContentHelper(kvp.Key, keyValueLine, comments));
                }
                content.AppendLine();
            }
            if (comments.Count > 0)
            {
                var remainingComments = comments.Where(c => c.CommentType == CommentType.FreeSpace &&
                        !string.IsNullOrEmpty(c.Content)).Select(c => $"; {c.Content}");
                var prefix = new StringBuilder();
                foreach (var line in remainingComments) prefix.AppendLine(line);
                if (prefix.Length > 0) content.Insert(0, prefix.ToString());
            }
            return content.ToString();
        }

        internal static bool IsCommentLine(string? trimmed)
        {
#if NETSTANDARD2_0
            return !(trimmed is null) && trimmed[0] == ';';
#else
            return !string.IsNullOrEmpty(trimmed) && trimmed[0] == ';';
#endif
        }

        internal static bool IsSectionLine(string? trimmed)
        {
            if (string.IsNullOrEmpty(trimmed)) return false;
            _ = TryParseLine(trimmed, out var section, out _);
#if NETSTANDARD2_0
            return section.Length > 1 && section[0] == '[' && section[section.Length - 1] == ']';
#else
            return section.Length > 1 && section[0] == '[' && section[^1] == ']';
#endif
        }

        internal static void HandleCommentLine(string[] lines, int index, string? trimmed, bool humanization, Comments? comments)
        {
            if (!humanization || comments is null) return;
#if NETSTANDARD2_0
            if (trimmed is null) return;
#else
            if (string.IsNullOrEmpty(trimmed)) return;
#endif
            string? nearestString = string.Empty;
            var commentType = CommentType.FreeSpace;
            if (index + 1 < lines.Length)
            {
                string line = lines[index + 1];
                if (!string.IsNullOrEmpty(line) && !line.StartsWith(";"))
                {
                    commentType = CommentType.Up;
                    nearestString = ParseNearestLine(line);
                }
            }
            if (index > 1)
            {
                string line = lines[index - 1];
                if (!string.IsNullOrEmpty(line) && !line.StartsWith(";"))
                {
                    commentType = CommentType.Down;
                    nearestString = ParseNearestLine(line);
                }
            }
            var commentText = trimmed.TrimStart(';').Trim();
            if (commentText.Length == 0) return;
            comments.Add(new Comment(nearestString, commentType, commentText));
        }

        internal static string HandleSectionLine(string? trimmed, bool humanization, Data data, Comments? comments)
        {
            _ = TryParseLine(trimmed, out var sectionPart, out var comment);
            var section = sectionPart.Trim('[', ']');
            if (!data.ContainsKey(section)) data[section] = new Dictionary<string, string>();
#if NETSTANDARD2_0
            if (humanization && !string.IsNullOrEmpty(comment) && !(comments is null))
#else
            if (humanization && !string.IsNullOrEmpty(comment) && comments is not null)
#endif
                comments.Add(new Comment(section, CommentType.Right, comment));
            return section;
        }

        internal static void HandleKeyValueLine(string? trimmed, string? currentSection, string? key, string? value, bool humanization, Data? data, Comments? comments)
        {
            if (data is null) return;
#if NETSTANDARD2_0
            if (currentSection is null || key is null) return;
#else
            if (string.IsNullOrEmpty(currentSection) || string.IsNullOrEmpty(key)) return;
#endif
            if (!data.ContainsKey(currentSection)) data[currentSection] = new Dictionary<string, string>();
            data[currentSection][key] = value ?? string.Empty;
            if (!humanization || comments is null) return;
            if (!TryParseLine(trimmed, out _, out var afterSemicolon)) return;
            if (!string.IsNullOrEmpty(afterSemicolon)) comments.Add(new Comment(key, CommentType.Right, afterSemicolon));
        }
    }
}
