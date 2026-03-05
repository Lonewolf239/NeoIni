using System;
using System.Globalization;
using System.Text;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniParser
{
    internal static string ValueToString<T>(T value)
    {
        var s = value?.ToString() ?? string.Empty;
        if (s.Length == 0) return s;
        var sb = new StringBuilder(s.Length * 2);
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
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    internal static string GetStringRaw(Data data, string section, string keyName)
    {
        string raw = data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val.Trim() : null;
        return Unescape(raw);
    }

    private static string Unescape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
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
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    internal static string GetContent(Data data)
    {
        var content = new StringBuilder();
        foreach (var section in data)
        {
            content.AppendLine($"[{section.Key}]");
            foreach (var kvp in section.Value)
                content.AppendLine($"{kvp.Key} = {kvp.Value}");
            content.AppendLine();
        }
        return content.ToString();
    }

    internal static T TryParseValue<T>(string value, T defaultValue, Action<Exception> onError)
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
            onError?.Invoke(ex);
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
        if (keySpan.IsEmpty || valueSpan.IsEmpty) return false;
        key = keySpan.ToString();
        value = valueSpan.ToString();
        return true;
    }
}
