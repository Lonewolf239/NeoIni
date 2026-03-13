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
/// <b>Version: 1.7.2-pre1</b>
/// <br/>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class NeoIniReader : IDisposable, IAsyncDisposable
{
    private readonly NeoIniFileProvider FileProvider;
    private readonly string FilePath;

    private readonly bool AutoEncryption = false;
    private bool CustomEncryptionPassword = false;

    private Dictionary<string, Dictionary<string, string>> Data;
    private List<Comment> Comments;
    private readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.NoRecursion);

    private bool Disposed = false;
    private int DisposeState = 0;

    private int HotReloadState = 0;
    private CancellationTokenSource HotReloadCts;
    private byte[] PrevHotReloadChecksum;
    private ManualResetEventSlim PauseHotReload = new(true);

    private bool HumanMode = false;

    /// <summary>
    /// Determines whether changes are automatically written to the disk after every modification.
	/// Default is <c>true</c>.
    /// </summary>
    public bool UseAutoSave { get; set; }

    /// <summary>
    /// Interval (in operations) between automatic saves when <see cref="UseAutoSave"/> is enabled.
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
    public bool UseAutoBackup { get; set; }

    /// <summary>
    /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="GetValue{T}"/>. 
	/// Default is <c>true</c>.
    /// </summary>
    public bool UseAutoAdd { get; set; }

    /// <summary>
    /// Determines whether a checksum is calculated and verified during file load and save operations to ensure data integrity.
    /// When enabled, the configuration file includes a checksum that detects corruption or tampering.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseChecksum
    {
        get => HumanMode ? false : _UseChecksum;
        set => _UseChecksum = value;
    }
    private bool _UseChecksum;

    /// <summary>
    /// Determines whether the configuration is automatically saved when the instance is disposed.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool SaveOnDispose { get; set; }

    /// <summary>
    /// Determines whether empty strings or null values are permitted for configuration keys.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool AllowEmptyValues { get; set; }

    /// <summary>Called before saving a file to disk</summary>
    public event EventHandler Saved;

    /// <summary>Called after successfully loading data from a file or reloading</summary>
    public event EventHandler Loaded;

    /// <summary>Called when the value of an existing key in a section changes</summary>
    public event EventHandler<KeyEventArgs> KeyChanged;

    /// <summary>Called when a key is renamed within a specific section.</summary>
    public event EventHandler<KeyRenamedEventArgs> KeyRenamed;

    /// <summary>Called when a new key is added to a section</summary>
    public event EventHandler<KeyEventArgs> KeyAdded;

    /// <summary>Called when a key is removed from a section</summary>
    public event EventHandler<KeyRemovedEventArgs> KeyRemoved;

    /// <summary>Called whenever a section changes (keys are changed/added/removed)</summary>
    public event EventHandler<SectionEventArgs> SectionChanged;

    /// <summary>Called when a section is renamed.</summary>
    public event EventHandler<SectionRenamedEventArgs> SectionRenamed;

    /// <summary>Called when a new section is added</summary>
    public event EventHandler<SectionEventArgs> SectionAdded;

    /// <summary>Called when a section is deleted</summary>
    public event EventHandler<SectionEventArgs> SectionRemoved;

    /// <summary>Called when the data is completely cleared</summary>
    public event EventHandler DataCleared;

    /// <summary>Called before automatic saving</summary>
    public event EventHandler AutoSave;

    /// <summary>Called when errors occur (parsing, saving, reading a file, etc.)</summary>
    public event EventHandler<ErrorEventArgs> Error
    {
        add => FileProvider.Error += value;
        remove => FileProvider.Error -= value;
    }

    /// <summary>Called when the checksum does not match when loading a file</summary>
    public event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch
    {
        add => FileProvider.ChecksumMismatch += value;
        remove => FileProvider.ChecksumMismatch -= value;
    }

    /// <summary>Called after each search</summary>
    public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

    private NeoIniReader(string path, EncryptionParameters encryptionParameters, bool autoEncryption, NeoIniReaderOptions options)
    {
        FilePath = path;
        AutoEncryption = autoEncryption;
        if (encryptionParameters.Key != null && encryptionParameters.Salt != null)
            FileProvider = new(FilePath, encryptionParameters, autoEncryption);
        else FileProvider = new(FilePath);
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
    public NeoIniReader(string path, NeoIniReaderOptions options = null) : this(path, new(null, null), false, options)
    {
        var neoIniData = FileProvider.GetData();
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

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
        this(path, autoEncryption ?
                NeoIniEncryptionProvider.GetEncryptionParameters(salt: NeoIniFileProvider.GetSalt(path)) :
                new(null, null), autoEncryption, options)
    {
        var neoIniData = FileProvider.GetData();
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

    /// <summary>Initializes a new instance of the <see cref="NeoIniReader"/> class with custom encryption</summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    public NeoIniReader(string path, string encryptionPassword, NeoIniReaderOptions options = null) :
        this(path, NeoIniEncryptionProvider.GetEncryptionParameters(encryptionPassword, NeoIniFileProvider.GetSalt(path)), false, options)
    {
        CustomEncryptionPassword = true;
        var neoIniData = FileProvider.GetData();
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="NeoIniReader"/> for the specified file path,
    /// with optional configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <param name="options">
    /// Optional reader configuration; if <c>null</c>, <see cref="NeoIniReaderOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, NeoIniReaderOptions options = null,
            CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(path, new(null, null), false, options);
        var neoIniData = await reader.FileProvider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
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
    /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, bool autoEncryption, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(path, autoEncryption ?
                NeoIniEncryptionProvider.GetEncryptionParameters(salt: NeoIniFileProvider.GetSalt(path)) : new(null, null), autoEncryption, options);
        var neoIniData = await reader.FileProvider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
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
    /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation,
    /// containing the initialized <see cref="NeoIniReader"/>.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(string path, string encryptionPassword, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(path,
                NeoIniEncryptionProvider.GetEncryptionParameters(encryptionPassword, NeoIniFileProvider.GetSalt(path)), false, options);
        reader.CustomEncryptionPassword = true;
        var neoIniData = await reader.FileProvider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
    }

    /// <summary>
    /// Enables human mode, allowing the software user to manually edit the INI configuration.
    /// </summary>
    /// <remarks>
    /// <para><b>Warning:</b> This is an <b>experimental</b> feature and its use is <b>not recommended for production environments</b>.</para>
    /// <para>
    /// Activating this mode automatically disables checksum validation (<c>UseChecksum = false</c>) 
    /// to accommodate manual modifications to the file. This mode cannot be used concurrently with encryption.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when encryption is enabled on the associated <see cref="FileProvider"/>.
    /// </exception>
    public static NeoIniReader CreateHumanMode(string path, NeoIniReaderOptions options = null)
    {
        NeoIniReader reader = new(path, new(null, null), false, options);
        reader.HumanMode = true;
        var neoIniData = reader.FileProvider.GetData(true);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
    }

    /// <summary>
    /// Asynchronously enables human mode, allowing the software user to manually edit the INI configuration.
    /// </summary>
    /// <param name="path">The file path to the INI configuration.</param>
    /// <param name="options">Optional settings to configure the new <see cref="NeoIniReader"/>.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newly created <see cref="NeoIniReader"/>.</returns>
    /// <remarks>
    /// <para><b>Warning:</b> This is an <b>experimental</b> feature and its use is <b>not recommended for production environments</b>.</para>
    /// <para>
    /// Activating this mode automatically disables checksum validation (<c>UseChecksum = false</c>) 
    /// to accommodate manual modifications to the file. This mode cannot be used concurrently with encryption.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when encryption is enabled on the associated <see cref="FileProvider"/>.
    /// </exception>
    public static async Task<NeoIniReader> CreateHumanModeAsync(string path, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(path, new(null, null), false, options);
        reader.HumanMode = true;
        var neoIniData = await reader.FileProvider.GetDataAsync(true, cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
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
        try { content = NeoIniParser.GetContent(Data, Comments, HumanMode); }
        finally { Lock.ExitReadLock(); }
        return content;
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

    /// <summary>Asynchronously releases managed resources and saves changes to the file</summary>
    public async ValueTask DisposeAsync() { await DisposeAsync(true); GC.SuppressFinalize(this); }

    private string ExtractContent()
    {
        Lock.EnterWriteLock();
        try
        {
            string content = SaveOnDispose ? NeoIniParser.GetContent(Data, Comments, HumanMode) : null;
            Data.Clear();
            Comments.Clear();
            return content;
        }
        finally { Lock.ExitWriteLock(); }
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref DisposeState, 1, 0) != 0) return;
        if (disposing)
        {
            StopHotReload();
            if (ExtractContent() is string content)
            {
                FileProvider.SaveFile(content, UseChecksum, UseAutoBackup);
                Saved?.Invoke(this, EventArgs.Empty);
            }
            DataCleared?.Invoke(this, EventArgs.Empty);
            Lock.Dispose();
        }
        Disposed = true;
    }

    /// <summary>Asynchronously releases managed resources and saves changes to the file</summary>
    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (Interlocked.CompareExchange(ref DisposeState, 1, 0) != 0) return;
        if (disposing)
        {
            StopHotReload();
            if (ExtractContent() is string content)
            {
                await FileProvider.SaveFileAsync(content, UseChecksum, UseAutoBackup, CancellationToken.None).ConfigureAwait(false);
                Saved?.Invoke(this, EventArgs.Empty);
            }
            DataCleared?.Invoke(this, EventArgs.Empty);
            Lock.Dispose();
        }
        Disposed = true;
    }

    private void ApplyOptions(NeoIniReaderOptions options)
    {
        options ??= new();
        UseAutoSave = options.UseAutoSave;
        AutoSaveInterval = options.AutoSaveInterval;
        UseAutoBackup = options.UseAutoBackup;
        UseAutoAdd = options.UseAutoAdd;
        UseChecksum = options.UseChecksum;
        SaveOnDispose = options.SaveOnDispose;
        AllowEmptyValues = options.AllowEmptyValues;
    }

    private void ThrowIfDisposed() { if (Disposed) throw new ObjectDisposedException(nameof(NeoIniReader)); }

    private void ThrowIfEmpty(string value)
    {
        if (!AllowEmptyValues && string.IsNullOrEmpty(value))
            throw new ArgumentException("The key value cannot be empty or null because AllowEmptyValues = false.", nameof(value));
    }

    private bool ShouldAutoSave()
    {
        if (!UseAutoSave) return false;
        if (AutoSaveInterval == 0) return true;
        return Interlocked.Increment(ref SaveIterationCounter) % AutoSaveInterval == 0;
    }

    private void DoAutoSave()
    {
        if (!ShouldAutoSave()) return;
        AutoSave?.Invoke(this, EventArgs.Empty);
        SaveFile();
    }

    private async Task DoAutoSaveAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!ShouldAutoSave()) return;
        AutoSave?.Invoke(this, EventArgs.Empty);
        await SaveFileAsync(ct).ConfigureAwait(false);
    }

    #region API

    /// <summary>Starts automatic hot reload monitoring for the underlying INI file.</summary>
    /// <param name="delayMs">The polling interval in milliseconds. Must be 1000 ms or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="delayMs"/> is less than 1000.</exception>
    public void StartHotReload(int delayMs)
    {
        ThrowIfDisposed();
        if (delayMs < 1000)
            throw new ArgumentOutOfRangeException(nameof(delayMs), "Delay must be at least 1000 ms for hot reload polling.");
        if (Interlocked.CompareExchange(ref HotReloadState, 1, 0) != 0) return;
        Lock.EnterWriteLock();
        try
        {
            HotReloadCts = new();
            PauseHotReload.Set();
        }
        finally { Lock.ExitWriteLock(); }
        PrevHotReloadChecksum = FileProvider.GetFileChecksum();
        _ = Task.Run(async () =>
        {
            try
            {
                while (!HotReloadCts.IsCancellationRequested)
                {
                    PauseHotReload.Wait(HotReloadCts.Token);
                    var checksum = FileProvider.GetFileChecksum();
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
    public void StopHotReload()
    {
        ThrowIfDisposed();
        if (Interlocked.CompareExchange(ref HotReloadState, 0, 1) != 1) return;
        HotReloadCts.Cancel();
    }

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
        FileProvider.SaveFile(content, UseChecksum, UseAutoBackup);
        if (HotReloadState == 1)
        {
            Lock.EnterWriteLock();
            try
            {
                PrevHotReloadChecksum = FileProvider.GetFileChecksum();
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
        await FileProvider.SaveFileAsync(content, UseChecksum, UseAutoBackup, cancellationToken).ConfigureAwait(false);
        if (HotReloadState == 1)
        {
            Lock.EnterWriteLock();
            try
            {
                PrevHotReloadChecksum = FileProvider.GetFileChecksum();
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
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
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
        cancellationToken.ThrowIfCancellationRequested();
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
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
        await AddKeyAsync(section, key, clampedValue);
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
            value = NeoIniParser.TryParseValue(stringValue, default(T), FileProvider.RaiseError);
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
                Lock.EnterWriteLock();
                try
                {
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                        ThrowIfEmpty(defaultValueString);
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
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, FileProvider.RaiseError);
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
                Lock.EnterWriteLock();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    stringValue = NeoIniParser.GetStringRaw(Data, section, key);
                    if (stringValue == null)
                    {
                        string defaultValueString = NeoIniParser.ValueToString(defaultValue);
                        ThrowIfEmpty(defaultValueString);
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
        return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, FileProvider.RaiseError);
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
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
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
        cancellationToken.ThrowIfCancellationRequested();
        bool keyExists = false;
        string valueString = NeoIniParser.ValueToString(value);
        ThrowIfEmpty(valueString);
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
        await SetValueAsync(section, key, clampedValue);
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
            var neoIniData = FileProvider.GetData(HumanMode);
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
            var neoIniData = await FileProvider.GetDataAsync(HumanMode, cancellationToken).ConfigureAwait(false);
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
        FileProvider.DeleteFile();
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
        FileProvider.DeleteBackup();
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
        if (!AutoEncryption) return "AutoEncryption is disabled";
        if (CustomEncryptionPassword) return "CustomEncryptionPassword is used. For security reasons, the password is not saved.";
        return NeoIniEncryptionProvider.GetEncryptionPassword(NeoIniFileProvider.GetSalt(FilePath));
    }

    #endregion
}
