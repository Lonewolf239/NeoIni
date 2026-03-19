using System;
using System.Collections.Generic;
using System.Linq;
using NeoIni.Models;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Core;

internal sealed class NeoIniReaderCore
{
    internal static bool SectionExists(Data? data, string? section) =>
        data is not null && section is not null && data.ContainsKey(section);

    internal static bool KeyExists(Data? data, string? section, string? key)
    {
        if (data is null || section is null || key is null) return false;
        return data.TryGetValue(section, out var sec) && sec.ContainsKey(key);
    }

    internal static void AddSection(Data? data, string? section)
    {
        if (data is not null && section is not null)
            _ = data.TryAdd(section, new Dictionary<string, string>());
    }

    internal static void AddKey(Data? data, string? section, string? key, string? value)
    {
        if (data is null || section is null || key is null) return;
        AddSection(data, section);
        data[section].TryAdd(key, value ?? string.Empty);
    }

    internal static bool SetValue(Data? data, string? section, string? key, string? value)
    {
        if (data is null || section is null || key is null) return false;
        if (!data.TryGetValue(section, out var sectionData))
        {
            data[section] = new Dictionary<string, string> { { key, value ?? string.Empty } };
            return false;
        }
        bool exists = sectionData.ContainsKey(key);
        sectionData[key] = value ?? string.Empty;
        return exists;
    }

    internal static void RemoveKey(Data? data, string? section, string? key)
    {
        if (data is null || section is null || key is null) return;
        if (data.TryGetValue(section, out var sec)) sec.Remove(key);
    }

    internal static void RemoveSection(Data? data, string? section) { if (data is not null && section is not null) data.Remove(section); }

    internal static string[] GetAllKeys(Data? data, string? section) =>
        data is not null && section is not null && data.TryGetValue(section, out var sec) ? sec.Keys.ToArray() : Array.Empty<string>();

    internal static Dictionary<string, string> GetSection(Data? data, string? section) =>
        data is not null && section is not null && data.TryGetValue(section, out var sec) ?
            new Dictionary<string, string>(sec) : new Dictionary<string, string>();

    internal static Dictionary<string, string> FindKeyInAllSections(Data? data, string? key)
    {
        Dictionary<string, string> results = new();
        if (data is null || key is null) return results;
        foreach (var section in data)
        {
            if (section.Value.TryGetValue(key, out var value))
                results[section.Key] = value;
        }
        return results;
    }

    internal static void ClearSection(Data? data, string? section)
    {
        if (data is not null && section is not null && data.TryGetValue(section, out var sec))
            sec.Clear();
    }

    internal static void RenameKey(Data? data, string? section, string? oldKey, string? newKey)
    {
        if (data is null || section is null || oldKey is null || newKey is null) return;
        if (data.TryGetValue(section, out var sec) && sec.TryGetValue(oldKey, out var value))
        {
            sec[newKey] = value;
            sec.Remove(oldKey);
        }
    }

    internal static void RenameSection(Data? data, string? oldSection, string? newSection)
    {
        if (data is null || oldSection is null || newSection is null) return;
        if (!data.TryGetValue(oldSection, out Dictionary<string, string>? value) || data.ContainsKey(newSection)) return;
        data[newSection] = value;
        data.Remove(oldSection);
    }

    internal static List<SearchResult> Search(Data? data, string? pattern)
    {
        List<SearchResult> results = new();
        if (data is null || string.IsNullOrEmpty(pattern)) return results;
        foreach (var section in data)
        {
            foreach (var kvp in section.Value)
            {
                if (kvp.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                    (kvp.Value?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true))
                    results.Add(new(section.Key, kvp.Key, NeoIniParser.GetStringRaw(kvp.Value) ?? string.Empty));
            }
        }
        return results;
    }
}
