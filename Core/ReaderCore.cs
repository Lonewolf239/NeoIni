using System;
using System.Collections.Generic;
using System.Linq;
using NeoIni.Models;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Core;

internal sealed class NeoIniReaderCore
{
    internal static bool SectionExists(Data data, string section) => data.ContainsKey(section);

    internal static bool KeyExists(Data data, string section, string key)
    {
        if (!SectionExists(data, section)) return false;
        return data[section].ContainsKey(key);
    }

    internal static void AddSection(Data data, string section) => data.TryAdd(section, new());

    internal static void AddKeyInSection(Data data, string section, string key, string value)
    {
        AddSection(data, section);
        data[section].TryAdd(key, value);
    }

    internal static bool SetKey(Data data, string section, string key, string value)
    {
        if (!data.TryGetValue(section, out var sectionData))
        {
            data[section] = new Dictionary<string, string> { { key, value } };
            return false;
        }
        bool exists = sectionData.ContainsKey(key);
        sectionData[key] = value;
        return exists;
    }

    internal static void RemoveKey(Data data, string section, string key) { if (KeyExists(data, section, key)) data[section].Remove(key); }

    internal static void RemoveSection(Data data, string section) { if (SectionExists(data, section)) data.Remove(section); }

    internal static string[] GetAllKeys(Data data, string section) =>
        data.TryGetValue(section, out var sec) ? sec.Keys.ToArray() : Array.Empty<string>();

    internal static Dictionary<string, string> GetSection(Data data, string section) =>
        data.TryGetValue(section, out var sec) ? new(sec) : new();

    internal static Dictionary<string, string> FindKeyInAllSections(Data data, string key)
    {
        Dictionary<string, string> results = new();
        foreach (var section in data)
        {
            if (section.Value.TryGetValue(key, out var value))
                results[section.Key] = value;
        }
        return results;
    }

    internal static void ClearSection(Data data, string section) { if (SectionExists(data, section)) data[section].Clear(); }

    internal static void RenameKey(Data data, string section, string oldKey, string newKey)
    {
        if (!KeyExists(data, section, oldKey)) return;
        data[section][newKey] = data[section][oldKey];
        data[section].Remove(oldKey);
    }

    internal static void RenameSection(Data data, string oldSection, string newSection)
    {
        if (!SectionExists(data, oldSection) || SectionExists(data, newSection)) return;
        data[newSection] = data[oldSection];
        data.Remove(oldSection);
    }

    internal static List<SearchResult> Search(Data data, string pattern)
    {
        List<SearchResult> results = new();
        if (string.IsNullOrEmpty(pattern)) return results;
        foreach (var section in data)
        {
            foreach (var kvp in section.Value)
            {
                if ((kvp.Key?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true) ||
                    (kvp.Value?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true))
                    results.Add(new(section.Key, kvp.Key, kvp.Value));
            }
        }
        return results;
    }
}
