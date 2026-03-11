using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniParser
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
                    sb.Append("\\r\\n");
                    i++;
                }
                else sb.Append("\\r");
                continue;
            }
            if (c == '\n') { sb.Append("\\n"); continue; }
            if (c == '\\') { sb.Append("\\\\"); continue; }
            sb.Append(c);
        }
        return FormatInvariant(sb);
    }

    internal static string GetStringRaw(Data data, string section, string keyName)
    {
        string raw = data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val.Trim() : null;
        return Unescape(raw);
    }

    private static string Unescape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        StringBuilder sb = new(s.Length);
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
                }
            }
            sb.Append(c);
        }
        return FormatInvariant(sb);
    }

    internal static string GetContent(Data data)
    {
        if (data == null || data.Count == 0) return string.Empty;
        var estimatedSize = 0;
        foreach (var section in data)
        {
            estimatedSize += section.Key.Length + 4;
            foreach (var kvp in section.Value)
                estimatedSize += kvp.Key.Length + (kvp.Value?.Length ?? 0) + 5;
        }
        StringBuilder content = new(estimatedSize);
        foreach (var section in data)
        {
            content.Append('[').Append(section.Key).Append(']').AppendLine();
            foreach (var kvp in section.Value)
                content.Append(kvp.Key).Append(" = ").AppendLine(kvp.Value);
            content.AppendLine();
        }
        return content.ToString();
    }

    internal static T TryParseValue<T>(string value, T defaultValue, EventHandler<ErrorEventArgs> onError)
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
        key = value = null;
        int eqIndex = line.IndexOf('=');
        if (eqIndex == -1) return false;
        ReadOnlySpan<char> keySpan = line[..eqIndex].Trim();
        ReadOnlySpan<char> valueSpan = line[(eqIndex + 1)..].Trim();
        if (keySpan.IsEmpty) return false;
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
}
