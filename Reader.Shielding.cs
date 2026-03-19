using System;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;

namespace NeoIni;

public partial class NeoIniReader
{
    internal string GetSaveContent()
    {
        if (HotReloadState == 1) { using (Lock.WriteLock()) PauseHotReload.Reset(); }
        using (Lock.ReadLock()) return NeoIniParser.GetContent(Data, Comments, UseShielding);
    }

    internal async Task<string> GetSaveContentAsync(CancellationToken ct = default)
    {
        if (HotReloadState == 1) await ExecuteWithWriteLockAsync(PauseHotReload.Reset, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        using (await Lock.ReadLockAsync(ct).ConfigureAwait(false))
            return NeoIniParser.GetContent(Data, Comments, UseShielding);
    }

    internal void FinalizeSave()
    {
        if (HotReloadState == 1)
        {
            using (Lock.WriteLock())
            {
                PrevHotReloadChecksum = Provider.GetStateChecksum();
                PauseHotReload.Set();
            }
        }
        Saved?.Invoke(this, EventArgs.Empty);
    }

    internal async Task FinalizeSaveAsync(CancellationToken ct = default)
    {
        if (HotReloadState == 1)
            await ExecuteWithWriteLockAsync(() =>
            {
                PrevHotReloadChecksum = Provider.GetStateChecksum();
                PauseHotReload.Set();
            }, ct).ConfigureAwait(false);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    internal void AddSectionHelper(string section)
    {
        ThrowIfDisposed();
        ValidateValue(section);
        using (Lock.WriteLock()) NeoIniReaderCore.AddSection(Data, section);
        SectionAdded?.Invoke(this, new(section));
    }

    internal async Task AddSectionHelperAsync(string section, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateValue(section);
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.AddSection(Data, section), ct).ConfigureAwait(false);
        SectionAdded?.Invoke(this, new(section));
    }

    internal void AddKeyHelper<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        string valueString = NeoIniParser.ValueToString(value);
        ValidateValue(valueString, true);
        using (Lock.WriteLock()) NeoIniReaderCore.AddKey(Data, section, key, valueString);
        KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal async Task AddKeyHelperAsync<T>(string section, string key, T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        ct.ThrowIfCancellationRequested();
        string valueString = NeoIniParser.ValueToString(value);
        ValidateValue(valueString, true);
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.AddKey(Data, section, key, valueString), ct).ConfigureAwait(false);
        KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal (bool, string?) GetValueHelper<T>(string section, string key, T? defaultValue)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        string? stringValue = null;
        bool valueAdded = false;
        using (Lock.ReadLock()) stringValue = NeoIniParser.GetStringRaw(Data, section, key);
        if (stringValue is null && UseAutoAdd)
        {
            using (Lock.WriteLock())
            {
                stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                if (stringValue is null)
                {
                    string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                    ValidateValue(defaultValueString, true);
                    NeoIniReaderCore.AddKey(Data, section, key, defaultValueString);
                    stringValue = defaultValueString;
                    valueAdded = true;
                }
            }
        }
        return (valueAdded, stringValue);
    }

    internal async Task<(bool, string?)> GetValueHelperAsync<T>(string section, string key, T? defaultValue, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        ct.ThrowIfCancellationRequested();
        string? stringValue = null;
        bool valueAdded = false;
        await ExecuteWithReadLockAsync(() => stringValue = NeoIniParser.GetStringRaw(Data, section, key), ct).ConfigureAwait(false);
        if (stringValue is null && UseAutoAdd)
            await ExecuteWithWriteLockAsync(() =>
                {
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue is null)
                    {
                        string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                        ValidateValue(defaultValueString, true);
                        NeoIniReaderCore.AddKey(Data, section, key, defaultValueString);
                        stringValue = defaultValueString;
                        valueAdded = true;
                    }
                }, ct).ConfigureAwait(false);
        return (valueAdded, stringValue);
    }

    internal void SetValueHelper<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ValidateValue(valueString, true);
        using (Lock.WriteLock()) keyExists = NeoIniReaderCore.SetValue(Data, section, key, valueString);
        if (keyExists) KeyChanged?.Invoke(this, new(section, key, valueString));
        else KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal async Task SetValueHelperAsync<T>(string section, string key, T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateTwoValue(section, key);
        ct.ThrowIfCancellationRequested();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ValidateValue(valueString, true);
        await ExecuteWithWriteLockAsync(() => keyExists = NeoIniReaderCore.SetValue(Data, section, key, valueString), ct).ConfigureAwait(false);
        if (keyExists) KeyChanged?.Invoke(this, new(section, key, valueString));
        else KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal void RemoveKeyHelper(string section, string key)
    {
        ThrowIfDisposed();
        using (Lock.WriteLock()) NeoIniReaderCore.RemoveKey(Data, section, key);
        KeyRemoved?.Invoke(this, new(section, key));
        SectionChanged?.Invoke(this, new(section));
    }

    internal async Task RemoveKeyHelperAsync(string section, string key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.RemoveKey(Data, section, key), ct).ConfigureAwait(false);
        KeyRemoved?.Invoke(this, new(section, key));
        SectionChanged?.Invoke(this, new(section));
    }

    internal void RemoveSectionHelper(string section)
    {
        ThrowIfDisposed();
        using (Lock.WriteLock()) NeoIniReaderCore.RemoveSection(Data, section);
        SectionRemoved?.Invoke(this, new(section));
    }

    internal async Task RemoveSectionHelperAsync(string section, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.RemoveSection(Data, section), ct).ConfigureAwait(false);
        SectionRemoved?.Invoke(this, new(section));
    }

    internal void ClearSectionHelper(string section)
    {
        ThrowIfDisposed();
        using (Lock.WriteLock()) NeoIniReaderCore.ClearSection(Data, section);
        SectionChanged?.Invoke(this, new(section));
    }

    internal async Task ClearSectionHelperAsync(string section, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.ClearSection(Data, section), ct).ConfigureAwait(false);
        SectionChanged?.Invoke(this, new(section));
    }

    internal void RenameKeyHelper(string section, string oldKey, string newKey)
    {
        ThrowIfDisposed();
        ValidateValue(section);
        ValidateTwoValue(oldKey, newKey);
        using (Lock.WriteLock()) NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey);
        KeyRenamed?.Invoke(this, new(section, oldKey, newKey));
    }

    internal async Task RenameKeyHelperAsync(string section, string oldKey, string newKey, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateValue(section);
        ValidateTwoValue(oldKey, newKey);
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey), ct).ConfigureAwait(false);
        KeyRenamed?.Invoke(this, new(section, oldKey, newKey));
    }

    internal void RenameSectionHelper(string oldSection, string newSection)
    {
        ThrowIfDisposed();
        ValidateTwoValue(oldSection, newSection);
        using (Lock.WriteLock()) NeoIniReaderCore.RenameSection(Data, oldSection, newSection);
        SectionRenamed?.Invoke(this, new(oldSection, newSection));
    }

    internal async Task RenameSectionHelperAsync(string oldSection, string newSection, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateTwoValue(oldSection, newSection);
        await ExecuteWithWriteLockAsync(() => NeoIniReaderCore.RenameSection(Data, oldSection, newSection), ct).ConfigureAwait(false);
        SectionRenamed?.Invoke(this, new(oldSection, newSection));
    }
}
