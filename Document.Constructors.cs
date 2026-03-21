using System;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni;

public partial class NeoIniDocument
{
    private NeoIniDocument(NeoIniOptions? options, IEncryptionProvider? encryptionProvider, string? path, EncryptionType encryptionType)
    {
        FilePath = path;
        EncryptionType = encryptionType;
        EncryptionProvider = encryptionProvider ?? new NeoIniEncryptionProvider();
        Provider = null!;
        ApplyOptions(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIniDocument"/> class using a custom data provider.
    /// This allows reading and saving configuration data from any source (e.g., file system, database, memory) 
    /// that implements the <see cref="INeoIniProvider"/> interface.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="options">Optional document configuration; if null, default settings are used.</param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the provider during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    public NeoIniDocument(INeoIniProvider? provider, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, null, null, EncryptionType.None)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        if (autoLoad) Load();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIniDocument"/> class using a custom data provider
    /// and a custom encryption provider. This allows full control over both storage and encryption.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting data.</param>
    /// <param name="options">Optional document configuration; if null, default settings are used.</param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the provider during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    public NeoIniDocument(INeoIniProvider? provider, IEncryptionProvider? encryptionProvider, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, encryptionProvider ?? throw new ArgumentNullException(nameof(encryptionProvider)), null, EncryptionType.None)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        if (autoLoad) Load();
    }

    /// <summary>
    /// Creates a new <see cref="NeoIniDocument"/> for the specified file path,
    /// with optional configuration options.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
	/// <param name="encryptionType">The encryption type to use for the file.</param>
    /// <param name="options">
    /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
    /// </param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the file during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    public NeoIniDocument(string? path, EncryptionType encryptionType = EncryptionType.None, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, null, path, encryptionType)
    {
        if (encryptionType == EncryptionType.None) Provider = new NeoIniFileProvider(path, EncryptionProvider);
        else if (encryptionType == EncryptionType.Auto)
        {
            var encryptionParameters = EncryptionProvider.GetEncryptionParameters(salt: NeoIniFileProvider.GetSalt(path));
            Provider = new NeoIniFileProvider(path, encryptionParameters, true, EncryptionProvider);
        }
        else throw new NotSupportedException(
            $"EncryptionType '{encryptionType}' is not supported in this constructor. " +
            $"Use the constructor with an encryption password parameter for {EncryptionType.Custom} encryption.");
        if (autoLoad) Load();
    }

    /// <summary>Initializes a new instance of the <see cref="NeoIniDocument"/> class with custom encryption</summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    /// <param name="options">
    /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
    /// </param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the file during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    public NeoIniDocument(string? path, string? encryptionPassword, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, null, path, EncryptionType.Custom)
    {
        var encryptionParameters = EncryptionProvider.GetEncryptionParameters(encryptionPassword, NeoIniFileProvider.GetSalt(path));
        Provider = new NeoIniFileProvider(path, encryptionParameters, false, EncryptionProvider);
        if (autoLoad) Load();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIniDocument"/> class for the specified file path
    /// using a custom encryption provider. The file will be encrypted with automatically generated parameters.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting the file.</param>
    /// <param name="options">
    /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
    /// </param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the file during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionProvider"/> is <c>null</c>.</exception>
    public NeoIniDocument(string? path, IEncryptionProvider? encryptionProvider, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, encryptionProvider, path, EncryptionType.Auto)
    {
        var encryptionParameters = EncryptionProvider.GetEncryptionParameters(salt: NeoIniFileProvider.GetSalt(path));
        Provider = new NeoIniFileProvider(path, encryptionParameters, true, EncryptionProvider);
        if (autoLoad) Load();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIniDocument"/> class for the specified file path
    /// using a custom encryption password and a custom encryption provider.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting the file.</param>
    /// <param name="options">
    /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
    /// </param>
    /// <param name="autoLoad">
    /// If <c>true</c>, the configuration data is loaded synchronously from the file during construction.
    /// If <c>false</c>, you must call <see cref="Reload"/> or <see cref="ReloadAsync"/> explicitly.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionProvider"/> is <c>null</c>.</exception>
    public NeoIniDocument(string? path, string? encryptionPassword, IEncryptionProvider? encryptionProvider, NeoIniOptions? options = null, bool autoLoad = true) :
        this(options, encryptionProvider, path, EncryptionType.Custom)
    {
        var encryptionParameters = EncryptionProvider.GetEncryptionParameters(encryptionPassword, NeoIniFileProvider.GetSalt(path));
        Provider = new NeoIniFileProvider(path, encryptionParameters, false, EncryptionProvider);
        if (autoLoad) Load();
    }
}
