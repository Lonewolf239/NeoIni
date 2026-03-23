using System;
using System.Text;
using NeoIni.Models;
using Comments = System.Collections.Generic.List<NeoIni.Models.Comment>;

namespace NeoIni.Core
{
    internal partial class NeoIniParser
    {
#if NETSTANDARD2_0
        private static string ParseShielding(string line)
        {
            int quoteIndex = line.IndexOf('"');
            if (quoteIndex == -1)
                return line;
            string valueString = line.Substring(quoteIndex + 1);
            int closeQuoteIndex = valueString.IndexOf('"');
            if (closeQuoteIndex == -1)
                return line;
            if (valueString.Substring(closeQuoteIndex + 1).Contains("\""))
                throw new MalformedShieldingException();
            return valueString.Substring(0, closeQuoteIndex);
        }
#else
        private static ReadOnlySpan<char> ParseShielding(ReadOnlySpan<char> line)
        {
            ReadOnlySpan<char> valueSpan;
            int quoteIndex = line.IndexOf('"');
            if (quoteIndex == -1) return line;
            valueSpan = line[(quoteIndex + 1)..];
            int closeQuoteIndex = valueSpan.IndexOf('"');
            if (closeQuoteIndex == -1) return line;
			if (valueSpan[(closeQuoteIndex + 1)..].Contains('"'))
				throw new MalformedShieldingException();
			valueSpan = valueSpan[..closeQuoteIndex];
            return valueSpan;
        }
#endif

        private static string? Unescape(string? s)
        {
#if NETSTANDARD2_0
            if (s is null) return s;
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                s = s.Substring(1, s.Length - 2);
#else
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s[1..^1];
#endif
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    switch (next)
                    {
                        case 'r':
                            if (i + 3 < s.Length && s[i + 2] == '\\' && s[i + 3] == 'n')
                            {
                                sb.Append('\r').Append('\n');
                                i += 3;
                            }
                            else
                            {
                                sb.Append('\r');
                                i++;
                            }
                            continue;
                        case 'n': sb.Append('\n'); i++; continue;
                        case '\\': sb.Append('\\'); i++; continue;
                        case '"': sb.Append('"'); i++; continue;
                    }
                }
                sb.Append(c);
            }
            return FormatInvariant(sb);
        }

        private static string GetContentHelper(string key, string lineContent, Comments? comments)
        {
            if (comments is null || comments.Count == 0) return lineContent;
            Comment? matched = null;
            foreach (var item in comments)
            {
                if (item.Line != key) continue;
                matched = item;
                break;
            }
            if (matched is null || string.IsNullOrEmpty(matched.Content)) return lineContent;
            comments.Remove(matched);
            return matched.CommentType switch
            {
                CommentType.Up => $"; {matched.Content}{Environment.NewLine}{lineContent}",
                CommentType.Right => $"{lineContent} ; {matched.Content}",
                CommentType.Down => $"{lineContent}{Environment.NewLine}; {matched.Content}",
                _ => $"; {matched.Content}{Environment.NewLine}"
            };
        }

        private static string? ParseNearestLine(string line)
        {
#if NETSTANDARD2_0
            if (TryMatchKey(line, out var key, out _)) return key;
#else
            if (TryMatchKey(line.AsSpan(), out var key, out _)) return key;
#endif
            return line.Trim('[', ']');
        }

        private static bool TryParseLine(string? trimmed, out string beforeSemicolon, out string afterSemicolon)
        {
            beforeSemicolon = string.Empty;
            afterSemicolon = string.Empty;
#if NETSTANDARD2_0
            if (trimmed is null) return false;
#else
            if (string.IsNullOrEmpty(trimmed)) return false;
#endif
            beforeSemicolon = trimmed;
            if (string.IsNullOrEmpty(trimmed)) return false;
            var semicolonIndex = trimmed.IndexOf(';');
            if (semicolonIndex < 0) return false;
#if NETSTANDARD2_0
            beforeSemicolon = trimmed.Substring(0, semicolonIndex).Trim();
            afterSemicolon = trimmed.Substring(semicolonIndex + 1).Trim();
#else
			beforeSemicolon = trimmed[..semicolonIndex].Trim();
			afterSemicolon = trimmed[(semicolonIndex + 1)..].Trim();
#endif
            return true;
        }
    }
}
