using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni;

/// <summary>
/// A class for working with INI files.
/// <br/>
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
/// <br/>
/// <b>Target Framework: .NET 6+</b>
/// <br/>
/// <b>Version: 1.6.0.1</b>
/// <br/>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class NeoIniReader : IDisposable
{
    private readonly NeoIniFileProvider FileProvider;
    private readonly string FilePath;

    private readonly bool AutoEncryption = false;
    private bool CustomEncryptionPassword = false;

    private Dictionary<string, Dictionary<string, string>> Data;
    private readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.NoRecursion);

    private bool Disposed = false;
    private int DisposeState = 0;

    /// <summary>
    /// Determines whether changes are automatically written to the disk after every modification.
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoSave;

    /// <summary>
    /// Interval (in operations) between automatic saves when <see cref="AutoSave"/> is enabled.
    /// Default value is 0.
    /// </summary>
    public int AutoSaveInterval
    {
        get => _AutoSaveInterval;
        set
        {
            if (value < 0) throw new ArgumentException("Interval cannot be negative.");
            _AutoSaveInterval = value;
        }
    }
    private int _AutoSaveInterval;
    private int SaveIterationCounter = 0;

    /// <summary>
    /// Determines whether backup files (.backup) are created during save operations.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool AutoBackup;

    /// <summary>
    /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="GetValue{T}"/>. 
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoAdd;

    /// <summary>
    /// Determines whether a checksum is calculated and verified during file load and save operations to ensure data integrity.
    /// When enabled, the configuration file includes a checksum that detects corruption or tampering.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseChecksum;

    /// <summary>
    /// Determines whether the configuration is automatically saved when the instance is disposed.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool SaveOnDispose;

    /// <summary>Called before saving a file to disk</summary>
    public Action OnSave;

    /// <summary>Called after successfully loading data from a file or reloading</summary>
    public Action OnLoad;

    /// <summary>Called when the value of an existing key in a section changes</summary>
    /// <param>Name of the section where the change occurred.</param>
    /// <param>Name of the changed key.</param>
    /// <param>New value of the key.</param>
    public Action<string, string, string> OnKeyChanged;

    /// <summary>Called when a key is renamed within a specific section.</summary>
    /// <param>The name of the section that contains the key.</param>
    /// <param>The original name of the key before renaming.</param>
    /// <param>The new name assigned to the key.</param>
    public Action<string, string, string> OnKeyRenamed;

    /// <summary>Called when a new key is added to a section</summary>
    /// <param>Name of the section to which the key was added.</param>
    /// <param>Name of the added key.</param>
    /// <param>Value of the added key.</param>
    public Action<string, string, string> OnKeyAdded;

    /// <summary>Called when a key is removed from a section</summary>
    /// <param>The name of the section from which the key was removed.</param>
    /// <param>The name of the removed key.</param>
    public Action<string, string> OnKeyRemoved;

    /// <summary>Called whenever a section changes (keys are changed/added/removed)</summary>
    /// <param>Name of the modified section.</param>
    public Action<string> OnSectionChanged;

    /// <summary>Called when a section is renamed.</summary>
    /// <param>The original name of the section.</param>
    /// <param>The new name assigned to the section.</param>
    public Action<string, string> OnSectionRenamed;

    /// <summary>Called when a new section is added</summary>
    /// <param>The name of the added section.</param>
    public Action<string> OnSectionAdded;

    /// <summary>Called when a section is deleted</summary>
    /// <param>Name of the deleted section.</param>
    public Action<string> OnSectionRemoved;

    /// <summary>Called when the data is completely cleared</summary>
    public Action OnDataCleared;

    /// <summary>Called before automatic saving</summary>
    public Action OnAutoSave;

    /// <summary>Called when errors occur (parsing, saving, reading a file, etc.)</summary>
    /// <param>An exception containing information about the error.</param>
    public Action<Exception> OnError
    {
        get => FileProvider?.OnError;
        set => FileProvider.OnError = value;
    }

    /// <summary>Called when the checksum does not match when loading a file</summary>
    /// <param>Expected checksum.</param>
    /// <param>Actual checksum.</param>
    public Action<byte[], byte[]> OnChecksumMismatch
    {
        get => FileProvider?.OnChecksumMismatch;
        set => FileProvider.OnChecksumMismatch = value;
    }

    /// <summary>Called after each search</summary>
    /// <param>Search pattern.</param>
    /// <param>Number of matches found.</param>
    public Action<string, int> OnSearchCompleted;

    private NeoIniReader(string path, (byte[] key, byte[] salt) encryptionData, bool autoEncryption, NeoIniReaderOptions options)
    {
        FilePath = path;
        AutoEncryption = autoEncryption;
        if (encryptionData.key != null && encryptionData.salt != null)
            FileProvider = new NeoIniFileProvider(FilePath, encryptionData, autoEncryption);
        else FileProvider = new NeoIniFileProvider(FilePath);
        ApplyOptions(options);
    }

    /// <summary>
    /// Creates a new <see cref="NeoIniReader"/> for the specified file path,
    /// with optional configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    public NeoIniReader(string path, NeoIniReaderOptions options = null) : this(path, (null, null), false, options) => Data = FileProvider.GetData();

    /// <summary>
    /// Creates a new <see cref="NeoIniReader"/> for the specified file path,
    /// with optional automatic encryption and configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <param name="autoEncryption">
    /// If <c>true</c>, the file is accessed through an encryption provider
    /// using an automatically generated encryption key.
    /// <para><b>Warning:</b> Enabling encryption ties the file to the specific machine/user 
    /// environment. The file will be unreadable on other computers due to machine-specific key generation!</para>
    /// </param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    public NeoIniReader(string path, bool autoEncryption, NeoIniReaderOptions options = null) :
        this(path, autoEncryption ? NeoIniEncryptionProvider.GetEncryptionKeyAndSalt(salt: NeoIniFileProvider.GetSalt(path)) : (null, null), autoEncryption, options) => Data = FileProvider.GetData();

    /// <summary>Initializes a new instance of the <see cref="NeoIniReader"/> class with custom encryption</summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    public NeoIniReader(string path, string encryptionPassword, NeoIniReaderOptions options = null) :
        this(path, NeoIniEncryptionProvider.GetEncryptionKeyAndSalt(encryptionPassword, NeoIniFileProvider.GetSalt(path)), false, options)
    {
        CustomEncryptionPassword = true;
        Data = FileProvider.GetData();
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="NeoIniReader"/> for the specified file path,
    /// with optional configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the asynchronous initialization.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, NeoIniReaderOptions options = null,
            CancellationToken cancellationToken = default)
    {
        var reader = new NeoIniReader(path, (null, null), false, options);
        reader.Data = await reader.FileProvider.GetDataAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="NeoIniReader"/> for the specified file path,
    /// with optional automatic encryption and configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <param name="autoEncryption">
    /// If <c>true</c>, the file is accessed through an encryption provider
    /// using an automatically generated encryption key.
    /// <para><b>Warning:</b> Enabling encryption ties the file to the specific machine/user
    /// environment. The file will be unreadable on other computers due to machine-specific key generation!</para>
    /// </param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the asynchronous initialization.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, bool autoEncryption, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        var reader = new NeoIniReader(path, autoEncryption ?
                NeoIniEncryptionProvider.GetEncryptionKeyAndSalt(salt: NeoIniFileProvider.GetSalt(path)) : (null, null), autoEncryption, options);
        reader.Data = await reader.FileProvider.GetDataAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="NeoIniReader"/> for the specified file path
    /// using a custom encryption password.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the asynchronous initialization.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, string encryptionPassword, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        var reader = new NeoIniReader(path,
                NeoIniEncryptionProvider.GetEncryptionKeyAndSalt(encryptionPassword, NeoIniFileProvider.GetSalt(path)), false, options);
        reader.CustomEncryptionPassword = true;
        reader.Data = await reader.FileProvider.GetDataAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

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
        try { content = NeoIniParser.GetContent(Data); }
        finally { Lock.ExitReadLock(); }
        return content;
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref DisposeState, 1, 0) != 0) return;
        string content = null;
        if (disposing)
        {
            Lock.EnterWriteLock();
            try
            {
                if (SaveOnDispose) content = NeoIniParser.GetContent(Data);
                Data.Clear();
            }
            finally { Lock.ExitWriteLock(); }
            if (SaveOnDispose && content is not null)
            {
                FileProvider.SaveFile(content, UseChecksum, AutoBackup);
                OnSave?.Invoke();
            }
            OnDataCleared?.Invoke();
            Lock.Dispose();
        }
        Disposed = true;
    }

    private void ApplyOptions(NeoIniReaderOptions options)
    {
        options ??= new NeoIniReaderOptions();
        AutoSave = options.AutoSave;
        AutoSaveInterval = options.AutoSaveInterval;
        AutoBackup = options.AutoBackup;
        AutoAdd = options.AutoAdd;
        UseChecksum = options.UseChecksum;
        SaveOnDispose = options.SaveOnDispose;
    }

    private void ThrowIfDisposed() { if (Disposed) throw new ObjectDisposedException(nameof(NeoIniReader)); }

    private bool ShouldAutoSave()
    {
        if (!AutoSave) return false;
        if (AutoSaveInterval == 0) return true;
        return Interlocked.Increment(ref SaveIterationCounter) % AutoSaveInterval == 0;
    }

    private void DoAutoSave()
    {
        if (!ShouldAutoSave()) return;
        OnAutoSave?.Invoke();
        SaveFile();
    }

    private async Task DoAutoSaveAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!ShouldAutoSave()) return;
        OnAutoSave?.Invoke();
        await SaveFileAsync(ct).ConfigureAwait(false);
    }

    #region API

    /// <summary>Saves the current data to an INI file with checksums and encryption applied, if enabled</summary>
    public void SaveFile()
    {
        ThrowIfDisposed();
        string content;
        Lock.EnterReadLock();
        try { content = NeoIniParser.GetContent(Data); }
        finally { Lock.ExitReadLock(); }
        FileProvider.SaveFile(content, UseChecksum, AutoBackup);
        OnSave?.Invoke();
    }

    /// <summary>Asynchronously saves the current data to the INI file</summary>
    public async Task SaveFileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        string content;
        Lock.EnterReadLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            content = NeoIniParser.GetContent(Data);
        }
        finally { Lock.ExitReadLock(); }
        await FileProvider.SaveFileAsync(content, UseChecksum, AutoBackup, cancellationToken).ConfigureAwait(false);
        OnSave?.Invoke();
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
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.AddSection(Data, section); }
        finally { Lock.ExitWriteLock(); }
        OnSectionAdded?.Invoke(section);
        DoAutoSave();
    }

    /// <summary>Asynchronously adds a new section to the file if it does not already exist</summary>
    /// <param name="section">The name of the new section.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddSectionAsync(string section, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.AddSection(Data, section);
        }
        finally { Lock.ExitWriteLock(); }
        OnSectionAdded?.Invoke(section);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a new key-value pair to a specified section</summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    public void AddKeyInSection<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        string valueString = NeoIniParser.ValueToString(value);
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.AddKeyInSection(Data, section, key, valueString); }
        finally { Lock.ExitWriteLock(); }
        OnKeyAdded?.Invoke(section, key, valueString);
        DoAutoSave();
    }

    /// <summary>Asynchronously adds a new key-value pair to a specified section</summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddKeyInSectionAsync<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        string valueString = NeoIniParser.ValueToString(value);
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.AddKeyInSection(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        OnKeyAdded?.Invoke(section, key, valueString);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to retrieve a value of the specified type from the INI data
    /// without modifying the underlying file or adding missing keys.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/> if retrieval fails.</returns>
    public T TryGetValue<T>(string section, string key, T defaultValue = default)
    {
        ThrowIfDisposed();
        string stringValue;
        Lock.EnterUpgradeableReadLock();
        try { stringValue = NeoIniParser.GetStringRaw(Data, section, key); }
        finally { Lock.ExitUpgradeableReadLock(); }
        if (stringValue == null) return defaultValue;
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, OnError);
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
            if (stringValue == null && AutoAdd)
            {
                Lock.EnterWriteLock();
                try
                {
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        NeoIniReaderCore.AddKeyInSection(Data, section, key, NeoIniParser.ValueToString(defaultValue));
                        valueAdded = true;
                    }
                }
                finally { Lock.ExitWriteLock(); }
            }
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        if (valueAdded) DoAutoSave();
        if (stringValue == null) return defaultValue;
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, OnError);
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
            if (stringValue == null && AutoAdd)
            {
                Lock.EnterWriteLock();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        NeoIniReaderCore.AddKeyInSection(Data, section, key, NeoIniParser.ValueToString(defaultValue));
                        valueAdded = true;
                    }
                }
                finally { Lock.ExitWriteLock(); }
            }
        }
        finally { Lock.ExitUpgradeableReadLock(); }
        if (valueAdded) await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
        if (stringValue == null) return defaultValue;
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, OnError);
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
    public T GetValueClamp<T>(string section, string key, T minValue, T maxValue, T defaultValue = default(T)) where T : IComparable<T>
    {
        T value = GetValue<T>(section, key, defaultValue);
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, minValue) < 0) return minValue;
        if (comparer.Compare(value, maxValue) > 0) return maxValue;
        return value;
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
    public async Task<T> GetValueClampAsync<T>(string section, string key, T minValue, T maxValue, T defaultValue,
            CancellationToken cancellationToken = default) where T : IComparable<T>
    {
        T value = await GetValueAsync(section, key, defaultValue, cancellationToken).ConfigureAwait(false);
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, minValue) < 0) return minValue;
        if (comparer.Compare(value, maxValue) > 0) return maxValue;
        return value;
    }

    /// <summary>Updates or creates a key-value pair in the specified section</summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    public void SetKey<T>(string section, string key, T value)
    {
        ThrowIfDisposed();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        Lock.EnterWriteLock();
        try { keyExists = NeoIniReaderCore.SetKey(Data, section, key, valueString); }
        finally { Lock.ExitWriteLock(); }
        if (keyExists) OnKeyChanged?.Invoke(section, key, valueString);
        else OnKeyAdded?.Invoke(section, key, valueString);
        DoAutoSave();
    }

    /// <summary>Asynchronously updates or creates a key-value pair in the specified section</summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetKeyAsync<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            keyExists = NeoIniReaderCore.SetKey(Data, section, key, valueString);
        }
        finally { Lock.ExitWriteLock(); }
        if (keyExists) OnKeyChanged?.Invoke(section, key, valueString);
        else OnKeyAdded?.Invoke(section, key, valueString);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
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
        OnKeyRemoved?.Invoke(section, key);
        OnSectionChanged?.Invoke(section);
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
        OnKeyRemoved?.Invoke(section, key);
        OnSectionChanged?.Invoke(section);
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
        OnSectionRemoved?.Invoke(section);
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
        OnSectionRemoved?.Invoke(section);
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
    public Dictionary<string, string> FindKeyInAllSections(string key)
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
        OnSectionChanged?.Invoke(section);
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
        OnSectionChanged?.Invoke(section);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames a key within a specific section by copying its value to a new key name and removing the old one</summary>
    /// <param name="section">The section containing the key to rename</param>
    /// <param name="oldKey">The current name of the key</param>
    /// <param name="newKey">The new name for the key</param>
    public void RenameKey(string section, string oldKey, string newKey)
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey); }
        finally { Lock.ExitWriteLock(); }
        OnKeyRenamed?.Invoke(section, oldKey, newKey);
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
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameKey(Data, section, oldKey, newKey);
        }
        finally { Lock.ExitWriteLock(); }
        OnKeyRenamed?.Invoke(section, oldKey, newKey);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames an entire section by moving all its contents to a new section name and removing the old one</summary>
    /// <param name="oldSection">The current name of the section</param>
    /// <param name="newSection">The new name for the section</param>
    public void RenameSection(string oldSection, string newSection)
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { NeoIniReaderCore.RenameSection(Data, oldSection, newSection); }
        finally { Lock.ExitWriteLock(); }
        OnSectionRenamed?.Invoke(oldSection, newSection);
        DoAutoSave();
    }

    /// <summary>Asynchronously renames an entire section</summary>
    /// <param name="oldSection">The current name of the section</param>
    /// <param name="newSection">The new name for the section</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RenameSectionAsync(string oldSection, string newSection, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        Lock.EnterWriteLock();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            NeoIniReaderCore.RenameSection(Data, oldSection, newSection);
        }
        finally { Lock.ExitWriteLock(); }
        OnSectionRenamed?.Invoke(oldSection, newSection);
        await DoAutoSaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Searches for keys or values matching a pattern across all sections and returns matching entries</summary>
    /// <param name="pattern">The search pattern to match against keys and values (case-insensitive)</param>
    /// <returns>A list of tuples containing (section, key, value) for all matches found</returns>
    public List<(string section, string key, string value)> Search(string pattern)
    {
        ThrowIfDisposed();
        List<(string, string, string)> results;
        Lock.EnterReadLock();
        try { results = NeoIniReaderCore.Search(Data, pattern); }
        finally { Lock.ExitReadLock(); }
        OnSearchCompleted?.Invoke(pattern, results.Count);
        return results;
    }

    /// <summary>Reloads data from the INI file, updating the internal data structure</summary>
    public void ReloadFromFile()
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { Data = FileProvider.GetData(); }
        finally { Lock.ExitWriteLock(); }
        OnLoad?.Invoke();
    }

    /// <summary>Removes the INI file from disk</summary>
    public void DeleteFile()
    {
        ThrowIfDisposed();
        FileProvider.DeleteFile();
    }

    /// <summary>Deletes the INI file from disk and clears the internal data structure</summary>
    public void DeleteFileWithData()
    {
        DeleteFile();
        Lock.EnterWriteLock();
        try { Data.Clear(); }
        finally { Lock.ExitWriteLock(); }
        OnDataCleared?.Invoke();
    }

    /// <summary>Deletes the backup file from disk</summary>
    public void DeleteBackup() => FileProvider.DeleteBackup();

    /// <summary>Clears the internal data structure</summary>
    public void Clear()
    {
        ThrowIfDisposed();
        Lock.EnterWriteLock();
        try { Data.Clear(); }
        finally { Lock.ExitWriteLock(); }
        OnDataCleared?.Invoke();
    }

    /// <summary>
    /// Returns the current encryption password if encryption is enabled, or a status message if disabled.
    /// Use the returned password in the NeoIniReader(path, password) constructor on a new machine
    /// to migrate the encrypted file without data loss.
    /// </summary>
    /// <returns>The generated encryption password or status message.</returns>
    public string GetEncryptionPassword()
    {
        if (!AutoEncryption) return "AutoEncryption is disabled";
        if (CustomEncryptionPassword) return "CustomEncryptionPassword is used. For security reasons, the password is not saved.";
        return NeoIniEncryptionProvider.GetEncryptionPassword(NeoIniFileProvider.GetSalt(FilePath));
    }

    #endregion
}
