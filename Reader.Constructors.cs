using System;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni;

public partial class NeoIniReader
{
    private NeoIniReader(string path, EncryptionParameters encryptionParameters, bool autoEncryption, NeoIniReaderOptions options)
    {
        FilePath = path;
        AutoEncryption = autoEncryption;
        if (encryptionParameters.Key != null && encryptionParameters.Salt != null)
            Provider = new NeoIniFileProvider(path, encryptionParameters, autoEncryption);
        else Provider = new NeoIniFileProvider(path);
        ApplyOptions(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIniReader"/> class using a custom data provider.
    /// This allows reading and saving configuration data from any source (e.g., file system, database, memory) 
    /// that implements the <see cref="INeoIniProvider"/> interface.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="options">Optional reader configuration; if null, default settings are used.</param>
    public NeoIniReader(INeoIniProvider provider, NeoIniReaderOptions options = null)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
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
        var neoIniData = Provider.GetData();
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
        var neoIniData = Provider.GetData();
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
        var neoIniData = Provider.GetData();
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

    /// <summary>
    /// Asynchronously creates and initializes a new <see cref="NeoIniReader"/> using a specified data provider.
    /// This method reads the initial configuration data from the provider during instantiation.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="options">Optional reader configuration; if null, default settings are used.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation, 
    /// containing the fully initialized <see cref="NeoIniReader"/> ready for use.
    /// </returns>
    public static async Task<NeoIniReader> CreateAsync(INeoIniProvider provider, NeoIniReaderOptions options = null,
            CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(provider, options);
        var neoIniData = await reader.Provider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
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
        var neoIniData = await reader.Provider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
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
        var neoIniData = await reader.Provider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
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
        var neoIniData = await reader.Provider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
    }

    /// <summary>
    /// Creates a new <see cref="NeoIniReader"/> using a specified data provider and enables human mode, 
    /// allowing the configuration to be formatted for easy reading and manual editing by users.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="options">Optional reader configuration; if null, default settings are used.</param>
    /// <returns>A newly created and initialized <see cref="NeoIniReader"/> instance configured for human mode.</returns>
    /// <remarks>
    /// <b>Warning:</b> This is an experimental feature and its use is not recommended for production environments.<br/>
    /// Activating this mode automatically disables checksum validation to accommodate manual 
    /// modifications to the data source. This mode cannot be used concurrently with encryption.
    /// </remarks>
    public static NeoIniReader CreateHumanMode(INeoIniProvider provider, NeoIniReaderOptions options = null)
    {
        NeoIniReader reader = new(provider, options);
        reader.HumanMode = true;
        var neoIniData = reader.Provider.GetData(true);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="NeoIniReader"/> using a specified data provider and enables human mode, 
    /// allowing the configuration to be formatted for easy reading and manual editing by users.
    /// </summary>
    /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
    /// <param name="options">Optional reader configuration; if null, default settings are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation. 
    /// The task result contains the newly created <see cref="NeoIniReader"/> configured for human mode.
    /// </returns>
    /// <remarks>
    /// <b>Warning:</b> This is an experimental feature and its use is not recommended for production environments.<br/>
    /// Activating this mode automatically disables checksum validation to accommodate manual 
    /// modifications to the data source. This mode cannot be used concurrently with encryption.
    /// </remarks>
    public static async Task<NeoIniReader> CreateHumanModeAsync(INeoIniProvider provider, NeoIniReaderOptions options = null,
            CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(provider, options);
        reader.HumanMode = true;
        var neoIniData = await reader.Provider.GetDataAsync(true, cancellationToken).ConfigureAwait(false);
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
    /// Thrown when encryption is enabled on the associated <see cref="Provider"/>.
    /// </exception>
    public static NeoIniReader CreateHumanMode(string path, NeoIniReaderOptions options = null)
    {
        NeoIniReader reader = new(path, new(null, null), false, options);
        reader.HumanMode = true;
        var neoIniData = reader.Provider.GetData(true);
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
    /// Thrown when encryption is enabled on the associated <see cref="Provider"/>.
    /// </exception>
    public static async Task<NeoIniReader> CreateHumanModeAsync(string path, NeoIniReaderOptions options = null,
        CancellationToken cancellationToken = default)
    {
        NeoIniReader reader = new(path, new(null, null), false, options);
        reader.HumanMode = true;
        var neoIniData = await reader.Provider.GetDataAsync(true, cancellationToken).ConfigureAwait(false);
        reader.Data = neoIniData.Data;
        reader.Comments = neoIniData.Comments;
        return reader;
    }
}
