using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Internal;
using NeoIni.Providers;

namespace NeoIni;

/// <summary>
/// A class for working with INI files.
/// <br/>
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
/// <br/>
/// <b>Target Framework: .NET 6+</b>
/// <br/>
/// <b>Version: 1.7.3</b>
/// <br/>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public partial class NeoIniReader : IDisposable, IAsyncDisposable
{
    /// <summary>Returns the INI data formatted as it would appear in the file</summary>
    /// <returns>
    /// A string containing the serialized INI content of this instance,
    /// formatted exactly as it would be written to the underlying file.
    /// </returns>
    public override string ToString()
    {
        ThrowIfDisposed();
        string content;
        Lock.EnterReadLock();
        try { content = NeoIniParser.GetContent(Data, Comments, HumanMode); }
        finally { Lock.ExitReadLock(); }
        return content;
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

    /// <summary>Asynchronously releases managed resources and saves changes to the file</summary>
    public async ValueTask DisposeAsync() { await DisposeAsync(true); GC.SuppressFinalize(this); }

    /// <summary>Starts automatic hot reload monitoring for the underlying INI file.</summary>
    /// <param name="delayMs">The polling interval in milliseconds. Must be 1000 ms or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="delayMs"/> is less than 1000.</exception>
    public void StartHotReload(int delayMs)
    {
        ThrowIfDisposed();
        if (delayMs < 1000)
            throw new InvalidHotReloadDelayException(nameof(delayMs));
        if (Interlocked.CompareExchange(ref HotReloadState, 1, 0) != 0) return;
        Lock.EnterWriteLock();
        try
        {
            HotReloadCts = new();
            PauseHotReload.Set();
        }
        finally { Lock.ExitWriteLock(); }
        PrevHotReloadChecksum = Provider.GetStateChecksum();
        _ = Task.Run(async () =>
        {
            try
            {
                while (!HotReloadCts.IsCancellationRequested)
                {
                    PauseHotReload.Wait(HotReloadCts.Token);
                    var checksum = Provider.GetStateChecksum();
                    if (!PrevHotReloadChecksum.SequenceEqual(checksum))
                    {
                        await ReloadFromFileAsync(HotReloadCts.Token);
                        Lock.EnterWriteLock();
                        try { PrevHotReloadChecksum = checksum; }
                        finally { Lock.ExitWriteLock(); }
                    }
                    await Task.Delay(delayMs, HotReloadCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            finally { Interlocked.Exchange(ref HotReloadState, 0); }
        }, HotReloadCts.Token);
    }

    /// <summary>Stops the hot reload monitoring if it is currently active.</summary>
    public void StopHotReload() { ThrowIfDisposed(); SafeStopHotReload(); }

    /// <summary>Saves the current data to an INI file with checksums and encryption applied, if enabled</summary>
    public void SaveFile()
    {
        ThrowIfDisposed();
        string content;
        Lock.EnterUpgradeableReadLock();
        try
        {
            if (HotReloadState == 1)
            {
                Lock.EnterWriteLock();
                try { PauseHotReload.Reset(); }
                finally { Lock.ExitWriteLock(); }
            }
            content = NeoIniParser.GetContent(Data, Comments, HumanMode);
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        Provider.Save(content, UseChecksum);
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

    /// <summary>Asynchronously saves the current data to the INI file</summary>
    public async Task SaveFileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        string content;
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
            content = NeoIniParser.GetContent(Data, Comments, HumanMode);
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        await Provider.SaveAsync(content, UseChecksum, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Determines whether a specific section exists in the loaded data</summary>
    /// <param name="section">The name of the section to search for.</param>
    /// <returns><c>true</c> if the section exists; otherwise, <c>false</c>.</returns>
    public bool SectionExists(string section)
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try { return NeoIniReaderCore.SectionExists(Data, section); }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>Determines whether a specific key exists within a given section</summary>
    /// <param name="section">The name of the section to search in.</param>
    /// <param name="key">The name of the key to search for.</param>
    /// <returns><c>true</c> if the key exists within the section; otherwise, <c>false</c>.</returns>
    public bool KeyExists(string section, string key)
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try { return NeoIniReaderCore.KeyExists(Data, section, key); }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>Adds a new section to the file if it does not already exist</summary>
    /// <param name="section">The name of the new section.</param>
    public void AddSection(string section)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfContainsUnsupportedChars(section);
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.AddSection(Data, section); }
        finally { Lock.ExitWriteLock(); }
        SectionAdded?.Invoke(this, new(section));
        DoAutoSave();
    }

    /// <summary>Asynchronously adds a new section to the file if it does not already exist</summary>
    /// <param name="section">The name of the new section.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddSectionAsync(string section, CancellationToken cancellationToken = default)
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
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a new key-value pair to a specified section</summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    public void AddKey<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(new[] { section, key, valueString });
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.AddKeyInSection(Data, section, key, valueString); }
        finally { Lock.ExitWriteLock(); }
        KeyAdded?.Invoke(this, new(section, key, valueString));
        DoAutoSave();
    }

    /// <summary>Asynchronously adds a new key-value pair to a specified section</summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddKeyAsync<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        cancellationToken.ThrowIfCancellationRequested();
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(new[] { section, key, valueString });
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.AddKeyInSection(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        KeyAdded?.Invoke(this, new(section, key, valueString));
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a new key-value pair to a specified section and clamps its value within the specified range.
    /// </summary>
    /// <typeparam name="T">The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="float"/>, etc.).</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="value">The value to assign to the key before clamping.</param>
    public void AddKeyClamped<T>(string section, string key, T minValue, T maxValue, T value) where T : IComparable<T>
    {
        T clampedValue = NeoIniParser.Clamp(value, minValue, maxValue);
        AddKey(section, key, clampedValue);
    }

    /// <summary>
    /// Asynchronously adds a new key-value pair to a specified section and clamps its value within the specified range.
    /// </summary>
    /// <typeparam name="T">The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="float"/>, etc.).</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="value">The value to assign to the key before clamping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddKeyClampedAsync<T>(string section, string key, T minValue, T maxValue, T value,
            CancellationToken cancellationToken = default) where T : IComparable<T>
    {
        T clampedValue = NeoIniParser.Clamp(value, minValue, maxValue);
        await AddKeyAsync(section, key, clampedValue, cancellationToken);
    }


    /// <summary>Attempts to retrieve a value of the specified type from the configuration data.</summary>
    /// <typeparam name="T">The expected type of the value to retrieve.</typeparam>
    /// <param name="section">The name of the section containing the key.</param>
    /// <param name="key">The specific key to look up.</param>
    /// <param name="value">
    /// When this method returns, contains the parsed value if the key is found,
    /// or the default value for <typeparamref name="T"/> if it is not.
    /// </param>
    /// <returns><c>true</c> if the key is found and read successfully; otherwise, <c>false</c>.</returns>
    public bool TryGetValue<T>(string section, string key, out T value)
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try
        {
            string stringValue = NeoIniParser.GetStringRaw(Data, section, key);
            if (stringValue == null)
            {
                value = default;
                return false;
            }
            value = NeoIniParser.TryParseValue(stringValue, default(T), Provider.RaiseError);
            return true;
        }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>
    /// Retrieves a value of a specified type from the INI file.
    /// Automatically parses the string value to the target type.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/> if retrieval fails.</returns>
    public T GetValue<T>(string section, string key, T defaultValue = default)
    {
        ThrowIfDisposed();
        string stringValue;
        bool valueAdded = false;
        Lock.EnterUpgradeableReadLock();
        try
        {
            stringValue = NeoIniParser.GetStringRaw(Data, section, key);
            if (stringValue == null && UseAutoAdd)
            {
                ThrowIfEmpty(section, false);
                ThrowIfEmpty(key, false);
                Lock.EnterWriteLock();
                try
                {
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                        ThrowIfEmpty(defaultValueString);
                        ThrowIfContainsUnsupportedChars(new[] { section, key, defaultValueString });
                        NeoIniReaderCore.AddKeyInSection(Data, section, key, defaultValueString);
                        valueAdded = true;
                    }
                }
                finally { Lock.ExitWriteLock(); }
            }
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        if (valueAdded) DoAutoSave();
        if (stringValue == null) return defaultValue;
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, Provider.RaiseError);
    }

    /// <summary>
    /// Asynchronously retrieves a value of a specified type from the INI file.
    /// Automatically parses the string value to the target type.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed value, or <paramref name="defaultValue"/> if retrieval fails.</returns>
    public async Task<T> GetValueAsync<T>(string section, string key, T defaultValue, CancellationToken cancellationToken = default)
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
                        ThrowIfContainsUnsupportedChars(new[] { section, key, defaultValueString });
                        NeoIniReaderCore.AddKeyInSection(Data, section, key, defaultValueString);
                        valueAdded = true;
                    }
                }
                finally { Lock.ExitWriteLock(); }
            }
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        if (valueAdded) await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
        if (stringValue == null) return defaultValue;
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, Provider.RaiseError);
    }

    /// <summary>
    /// Retrieves a numeric or comparable value from the INI file and clamps it within the specified range.
    /// </summary>
    /// <typeparam name="T">
    /// The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="DateTime"/>, etc.).
    /// </typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="defaultValue">
    /// The value to return if the key or section does not exist, or if parsing fails.
    /// </param>
    /// <returns>
    /// The parsed and clamped value. If retrieval or parsing fails, returns <paramref name="defaultValue"/>.
    /// </returns>
    public T GetValueClamped<T>(string section, string key, T minValue, T maxValue, T defaultValue = default(T)) where T : IComparable<T>
    {
        T value = GetValue<T>(section, key, defaultValue);
        return NeoIniParser.Clamp(value, minValue, maxValue);
    }

    /// <summary>
    /// Asynchronously retrieves a numeric or comparable value from the INI file and clamps it within the specified range.
    /// </summary>
    /// <typeparam name="T">
    /// The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="DateTime"/>, etc.).
    /// </typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="defaultValue">
    /// The value to return if the key or section does not exist, or if parsing fails.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the parsed and clamped value,
    /// or <paramref name="defaultValue"/> if retrieval fails.
    /// </returns>
    public async Task<T> GetValueClampedAsync<T>(string section, string key, T minValue, T maxValue, T defaultValue,
            CancellationToken cancellationToken = default) where T : IComparable<T>
    {
        T value = await GetValueAsync(section, key, defaultValue, cancellationToken).ConfigureAwait(false);
        return NeoIniParser.Clamp(value, minValue, maxValue);
    }

    /// <summary>Updates or creates a key-value pair in the specified section</summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    public void SetValue<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(new[] { section, key, valueString });
        Lock.EnterWriteLock();
        try { keyExists = NeoIniReaderCore.SetKey(Data, section, key, valueString); }
        finally { Lock.ExitWriteLock(); }
        if (keyExists) KeyChanged?.Invoke(this, new(section, key, valueString));
        else KeyAdded?.Invoke(this, new(section, key, valueString));
        DoAutoSave();
    }

    /// <summary>Asynchronously updates or creates a key-value pair in the specified section</summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetValueAsync<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section, false);
        ThrowIfEmpty(key, false);
        cancellationToken.ThrowIfCancellationRequested();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
        ThrowIfContainsUnsupportedChars(new[] { section, key, valueString });
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            keyExists = NeoIniReaderCore.SetKey(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        if (keyExists) KeyChanged?.Invoke(this, new(section, key, valueString));
        else KeyAdded?.Invoke(this, new(section, key, valueString));
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates or creates a key-value pair in the specified section, clamping its value within the specified range.
    /// </summary>
    /// <typeparam name="T">The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="float"/>, etc.).</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="value">The value to write to the file before clamping.</param>
    public void SetValueClamped<T>(string section, string key, T minValue, T maxValue, T value) where T : IComparable<T>
    {
        T clampedValue = NeoIniParser.Clamp(value, minValue, maxValue);
        SetValue(section, key, clampedValue);
    }

    /// <summary>
    /// Asynchronously updates or creates a key-value pair in the specified section, clamping its value within the specified range.
    /// </summary>
    /// <typeparam name="T">The comparable type of the value (e.g., <see cref="int"/>, <see cref="double"/>, <see cref="float"/>, etc.).</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="value">The value to write to the file before clamping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetValueClampedAsync<T>(string section, string key, T minValue, T maxValue, T value,
            CancellationToken cancellationToken = default) where T : IComparable<T>
    {
        T clampedValue = NeoIniParser.Clamp(value, minValue, maxValue);
        await SetValueAsync(section, key, clampedValue, cancellationToken);
    }

    /// <summary>Removes a specific key from a section in the INI file</summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    public void RemoveKey(string section, string key)
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RemoveKey(Data, section, key); }
        finally { Lock.ExitWriteLock(); }
        KeyRemoved?.Invoke(this, new(section, key));
        SectionChanged?.Invoke(this, new(section));
        DoAutoSave();
    }

    /// <summary>Asynchronously removes a specific key from a section in the INI file</summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveKeyAsync(string section, string key, CancellationToken cancellationToken = default)
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
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes an entire section and all its keys from the INI file</summary>
    /// <param name="section">The name of the section to remove.</param>
    public void RemoveSection(string section)
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RemoveSection(Data, section); }
        finally { Lock.ExitWriteLock(); }
        SectionRemoved?.Invoke(this, new(section));
        DoAutoSave();
    }

    /// <summary>Asynchronously removes an entire section and all its keys from the INI file</summary>
    /// <param name="section">The name of the section to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveSectionAsync(string section, CancellationToken cancellationToken = default)
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
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns an array of all sections contained in the INI file</summary>
    /// <returns>An array of strings containing the names of all sections.</returns>
    public string[] GetAllSections()
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try { return Data.Keys.ToArray(); }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>Returns an array of all keys in the specified INI file section</summary>
    /// <param name="section">Name of the section to receive keys from.</param>
    /// <returns>An array of strings containing the names of all keys in the section, or an empty array if the section does not exist.</returns>
    public string[] GetAllKeys(string section)
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try { return NeoIniReaderCore.GetAllKeys(Data, section); }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>Returns a dictionary containing all key-value pairs from the specified section</summary>
    /// <param name="section">The name of the section to retrieve.</param>
    /// <returns>A read-only copy of the section's key-value pairs, or an empty dictionary if the section does not exist.</returns>
    public Dictionary<string, string> GetSection(string section)
    {
        ThrowIfDisposed();
        Lock.EnterReadLock();
        try { return NeoIniReaderCore.GetSection(Data, section); }
        finally { Lock.ExitReadLock(); }
    }

    /// <summary>Searches for a specific key across all sections and returns a dictionary mapping section names to their corresponding values</summary>
    /// <param name="key">The key name to search for across all sections.</param>
    /// <returns>A dictionary where keys are section names and values are the corresponding key values found, or an empty dictionary if no matches are found.</returns>
    public Dictionary<string, string> FindKey(string key)
    {
        ThrowIfDisposed();
        Dictionary<string, string> results;
        Lock.EnterReadLock();
        try { results = NeoIniReaderCore.FindKeyInAllSections(Data, key); }
        finally { Lock.ExitReadLock(); }
        return results;
    }

    /// <summary>Clears all keys from the specified section while keeping the section itself intact</summary>
    /// <param name="section">The name of the section to clear.</param>
    public void ClearSection(string section)
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.ClearSection(Data, section); }
        finally { Lock.ExitWriteLock(); }
        SectionChanged?.Invoke(this, new(section));
        DoAutoSave();
    }

    /// <summary>Asynchronously clears all keys from the specified section</summary>
    /// <param name="section">The name of the section to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ClearSectionAsync(string section, CancellationToken cancellationToken = default)
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
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames a key within a specific section by copying its value to a new key name and removing the old one</summary>
    /// <param name="section">The section containing the key to rename</param>
    /// <param name="oldKey">The current name of the key</param>
    /// <param name="newKey">The new name for the key</param>
    public void RenameKey(string section, string oldKey, string newKey)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section);
        ThrowIfEmpty(oldKey);
        ThrowIfEmpty(newKey);
        ThrowIfContainsUnsupportedChars(new[] { section, oldKey, newKey });
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey); }
        finally { Lock.ExitWriteLock(); }
        KeyRenamed?.Invoke(this, new(section, oldKey, newKey));
        DoAutoSave();
    }

    /// <summary>Asynchronously renames a key within a specific section</summary>
    /// <param name="section">The section containing the key to rename</param>
    /// <param name="oldKey">The current name of the key</param>
    /// <param name="newKey">The new name for the key</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RenameKeyAsync(string section, string oldKey, string newKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(section);
        ThrowIfEmpty(oldKey);
        ThrowIfEmpty(newKey);
        ThrowIfContainsUnsupportedChars(new[] { section, oldKey, newKey });
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey);
        }
        finally { Lock.ExitWriteLock(); }
        KeyRenamed?.Invoke(this, new(section, oldKey, newKey));
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames an entire section by moving all its contents to a new section name and removing the old one</summary>
    /// <param name="oldSection">The current name of the section</param>
    /// <param name="newSection">The new name for the section</param>
    public void RenameSection(string oldSection, string newSection)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(oldSection);
        ThrowIfEmpty(newSection);
        ThrowIfContainsUnsupportedChars(new[] { oldSection, newSection });
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RenameSection(Data, oldSection, newSection); }
        finally { Lock.ExitWriteLock(); }
        SectionRenamed?.Invoke(this, new(oldSection, newSection));
        DoAutoSave();
    }

    /// <summary>Asynchronously renames an entire section</summary>
    /// <param name="oldSection">The current name of the section</param>
    /// <param name="newSection">The new name for the section</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RenameSectionAsync(string oldSection, string newSection, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfEmpty(oldSection);
        ThrowIfEmpty(newSection);
        ThrowIfContainsUnsupportedChars(new[] { oldSection, newSection });
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameSection(Data, oldSection, newSection);
        }
        finally { Lock.ExitWriteLock(); }
        SectionRenamed?.Invoke(this, new(oldSection, newSection));
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Searches for keys or values matching a pattern across all sections and returns matching entries</summary>
    /// <param name="pattern">The search pattern to match against keys and values (case-insensitive)</param>
    /// <returns>A list of <see cref="SearchResult"/> objects representing all matches found</returns>
    public List<SearchResult> Search(string pattern)
    {
        ThrowIfDisposed();
        List<SearchResult> results;
        Lock.EnterReadLock();
        try { results = NeoIniReaderCore.Search(Data, pattern); }
        finally { Lock.ExitReadLock(); }
        SearchCompleted?.Invoke(this, new(pattern, results.Count));
        return results;
    }

    /// <summary>Reloads data from the INI file, updating the internal data structure</summary>
    public void ReloadFromFile()
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try
        {
            var neoIniData = Provider.GetData(HumanMode);
            Data = neoIniData.Data;
            Comments = neoIniData.Comments;
        }
        finally { Lock.ExitWriteLock(); }
        Loaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Asynchronously reloads data from the INI file, updating the internal data structure.</summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    public async Task ReloadFromFileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            var neoIniData = await Provider.GetDataAsync(HumanMode, cancellationToken).ConfigureAwait(false);
            Data = neoIniData.Data;
            Comments = neoIniData.Comments;
        }
        finally { Lock.ExitWriteLock(); }
        Loaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the INI file from disk</summary>
    public void DeleteFile()
    {
        ThrowIfDisposed();
        if (Provider is NeoIniFileProvider fileProvider) fileProvider.DeleteFile();
        else throw new UnsupportedProviderOperationException();
    }

    /// <summary>Deletes the INI file from disk and clears the internal data structure</summary>
    public void DeleteFileWithData()
    {
        DeleteFile();
        Lock.EnterWriteLock();
        try
        {
            Data.Clear();
            Comments.Clear();
        }
        finally { Lock.ExitWriteLock(); }
        DataCleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes the backup file from disk</summary>
    public void DeleteBackup()
    {
        ThrowIfDisposed();
        if (Provider is NeoIniFileProvider fileProvider) fileProvider.DeleteBackup();
        else throw new UnsupportedProviderOperationException();
    }

    /// <summary>Clears the internal data structure</summary>
    public void Clear()
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try
        {
            Data.Clear();
            Comments.Clear();
        }
        finally { Lock.ExitWriteLock(); }
        DataCleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the current encryption password if encryption is enabled, or a status message if disabled.
    /// Use the returned password in the NeoIniReader(path, password) constructor on a new machine
    /// to migrate the encrypted file without data loss.
    /// </summary>
    /// <returns>The generated encryption password or status message.</returns>
    public string GetEncryptionPassword()
    {
        ThrowIfDisposed();
        if (Provider is not NeoIniFileProvider)
            throw new UnsupportedProviderOperationException("Retrieving the auto-generated encryption password is only supported when using the default file provider.");
        if (!AutoEncryption) return "AutoEncryption is disabled";
        if (CustomEncryptionPassword) return "CustomEncryptionPassword is used. For security reasons, the password is not saved.";
        return NeoIniEncryptionProvider.GetEncryptionPassword(NeoIniFileProvider.GetSalt(FilePath));
    }
}
