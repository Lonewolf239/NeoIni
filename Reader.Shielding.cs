using System;
using System.Threading;
using NeoIni.Core;

namespace NeoIni;

public partial class NeoIniReader
{
    internal string GetSaveContent(CancellationToken cancellationToken = default)
    {
        Lock.EnterUpgradeableReadLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (HotReloadState == 1)
            {
                Lock.EnterWriteLock();
                try { PauseHotReload.Reset(); }
                finally { Lock.ExitWriteLock(); }
            }
            return NeoIniParser.GetContent(Data, Comments, HumanMode, UseShielding);
        }
        finally { Lock.ExitUpgradeableReadLock(); }
    }

    internal void FinalizeSave()
    {
        if (HotReloadState == 1)
        {
            Lock.EnterWriteLock();
            try
            {
                PrevHotReloadChecksum = Provider.GetStateChecksum();
                PauseHotReload.Set();
            }
            finally { Lock.ExitWriteLock(); }
        }
        Saved?.Invoke(this, EventArgs.Empty);
    }

    internal void AddSectionHelper(string section, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfContainsUnsupportedChars(section);
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.AddSection(Data, section);
        }
        finally { Lock.ExitWriteLock(); }
        SectionAdded?.Invoke(this, new(section));
    }

    internal void AddKeyHelper<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        cancellationToken.ThrowIfCancellationRequested();
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(section);
        ThrowIfContainsUnsupportedChars(key);
        ThrowIfContainsUnsupportedChars(valueString);
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.AddKey(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal (bool, string) GetValueHelper<T>(string section, string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        string stringValue;
        bool valueAdded = false;
        Lock.EnterUpgradeableReadLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            stringValue = NeoIniParser.GetStringRaw(Data, section, key);
            if (stringValue == null && UseAutoAdd)
            {
                ThrowIfEmpty(section, false);
                ThrowIfEmpty(key, false);
                Lock.EnterWriteLock();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                        ThrowIfEmpty(defaultValueString);
                        ThrowIfContainsUnsupportedChars(section);
                        ThrowIfContainsUnsupportedChars(key);
                        ThrowIfContainsUnsupportedChars(defaultValueString);
                        NeoIniReaderCore.AddKey(Data, section, key, defaultValueString);
                        valueAdded = true;
                    }
                }
                finally { Lock.ExitWriteLock(); }
            }
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        return (valueAdded, stringValue);
    }

    internal void SetValueHelper<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        cancellationToken.ThrowIfCancellationRequested();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(section);
        ThrowIfContainsUnsupportedChars(key);
        ThrowIfContainsUnsupportedChars(valueString);
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            keyExists = NeoIniReaderCore.SetValue(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        if (keyExists) KeyChanged?.Invoke(this, new(section, key, valueString));
        else KeyAdded?.Invoke(this, new(section, key, valueString));
    }

    internal void RemoveKeyHelper(string section, string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RemoveKey(Data, section, key);
        }
        finally { Lock.ExitWriteLock(); }
        KeyRemoved?.Invoke(this, new(section, key));
        SectionChanged?.Invoke(this, new(section));
    }

    internal void RemoveSectionHelper(string section, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RemoveSection(Data, section);
        }
        finally { Lock.ExitWriteLock(); }
        SectionRemoved?.Invoke(this, new(section));
    }

    internal void ClearSectionHelper(string section, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.ClearSection(Data, section);
        }
        finally { Lock.ExitWriteLock(); }
        SectionChanged?.Invoke(this, new(section));
    }

    internal void RenameKeyHelper(string section, string oldKey, string newKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section);
        ThrowIfEmpty(oldKey);
        ThrowIfEmpty(newKey);
        ThrowIfContainsUnsupportedChars(section);
        ThrowIfContainsUnsupportedChars(oldKey);
        ThrowIfContainsUnsupportedChars(newKey);
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey);
        }
        finally { Lock.ExitWriteLock(); }
        KeyRenamed?.Invoke(this, new(section, oldKey, newKey));
    }

    internal void RenameSectionHelper(string oldSection, string newSection, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(oldSection);
        ThrowIfEmpty(newSection);
        ThrowIfContainsUnsupportedChars(oldSection);
        ThrowIfContainsUnsupportedChars(newSection);
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameSection(Data, oldSection, newSection);
        }
        finally { Lock.ExitWriteLock(); }
        SectionRenamed?.Invoke(this, new(oldSection, newSection));
    }
}
