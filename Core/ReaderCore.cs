using System;
using System.Collections.Generic;
using System.Linq;
using NeoIni.Models;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni.Core
{
    internal sealed class NeoIniReaderCore
    {
#if NETSTANDARD2_0
        internal static bool SectionExists(Data? data, string? section) =>
            !(data is null) && !(section is null) && data.ContainsKey(section);
#else
        internal static bool SectionExists(Data? data, string? section) =>
            data is not null && section is not null && data.ContainsKey(section);
#endif

        internal static bool KeyExists(Data? data, string? section, string? key)
        {
            if (data is null || section is null || key is null) return false;
            return data.TryGetValue(section, out var sec) && sec.ContainsKey(key);
        }

        internal static void AddSection(Data? data, string? section)
        {
#if NETSTANDARD2_0
            if (!(data is null) && !(section is null) && !data.ContainsKey(section))
                data.Add(section, new Dictionary<string, string>());
#else
            if (data is not null && section is not null)
                _ = data.TryAdd(section, new Dictionary<string, string>());
#endif
        }

        internal static void AddKey(Data? data, string? section, string? key, string? value)
        {
            if (data is null || section is null || key is null) return;
            AddSection(data, section);
#if NETSTANDARD2_0
            if (!data[section].ContainsKey(key))
                data[section].Add(key, value ?? string.Empty);
#else
            data[section].TryAdd(key, value ?? string.Empty);
#endif
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

#if NETSTANDARD2_0
        internal static void RemoveSection(Data? data, string? section) { if (!(data is null) && !(section is null)) data.Remove(section); }
#else
        internal static void RemoveSection(Data? data, string? section) { if (data is not null && section is not null) data.Remove(section); }
#endif

#if NETSTANDARD2_0
        internal static string[] GetAllKeys(Data? data, string? section) =>
            !(data is null) && !(section is null) && data.TryGetValue(section, out var sec) ? sec.Keys.ToArray() : Array.Empty<string>();
#else
        internal static string[] GetAllKeys(Data? data, string? section) =>
            data is not null && section is not null && data.TryGetValue(section, out var sec) ? sec.Keys.ToArray() : Array.Empty<string>();
#endif

#if NETSTANDARD2_0
        internal static Dictionary<string, string> GetSection(Data? data, string? section) =>
            !(data is null) && !(section is null) && data.TryGetValue(section, out var sec) ?
                new Dictionary<string, string>(sec) : new Dictionary<string, string>();
#else
        internal static Dictionary<string, string> GetSection(Data? data, string? section) =>
            data is not null && section is not null && data.TryGetValue(section, out var sec) ?
                new Dictionary<string, string>(sec) : new Dictionary<string, string>();
#endif

        internal static Dictionary<string, string> FindKeyInAllSections(Data? data, string? key)
        {
            var results = new Dictionary<string, string>();
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
#if NETSTANDARD2_0
            if (!(data is null) && !(section is null) && data.TryGetValue(section, out var sec))
                sec.Clear();
#else
            if (data is not null && section is not null && data.TryGetValue(section, out var sec))
                sec.Clear();
#endif
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
            var results = new List<SearchResult>();
            if (data is null || string.IsNullOrEmpty(pattern)) return results;
            foreach (var section in data)
            {
                foreach (var kvp in section.Value)
                {
#if NETSTANDARD2_0
                    if (kvp.Key.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (kvp.Value != null && kvp.Value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                        results.Add(new SearchResult(section.Key, kvp.Key, NeoIniParser.GetStringRaw(kvp.Value) ?? string.Empty));
#else
					if (kvp.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
							(kvp.Value?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true))
						results.Add(new(section.Key, kvp.Key, NeoIniParser.GetStringRaw(kvp.Value) ?? string.Empty));
#endif
                }
            }
            return results;
        }
    }
}
