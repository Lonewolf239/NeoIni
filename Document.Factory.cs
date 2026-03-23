using System;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni
{
    public partial class NeoIniDocument
    {
        /// <summary>
        /// Asynchronously creates and initializes a new <see cref="NeoIniDocument"/> using a specified data provider.
        /// This method reads the initial configuration data from the provider during instantiation.
        /// </summary>
        /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
        /// <param name="options">Optional document configuration; if null, default settings are used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the provider during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation, 
        /// containing the fully initialized <see cref="NeoIniDocument"/> ready for use.
        /// </returns>
        public static async Task<NeoIniDocument> CreateAsync(INeoIniProvider? provider, NeoIniOptions? options = null, bool autoLoad = true,
                CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(provider, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> using a custom data provider
        /// and a custom encryption provider.
        /// </summary>
        /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
        /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting data.</param>
        /// <param name="options">Optional document configuration; if null, default settings are used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the provider during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation,
        /// containing the initialized <see cref="NeoIniDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> or <paramref name="encryptionProvider"/> is null.</exception>
        public static async Task<NeoIniDocument> CreateAsync(INeoIniProvider? provider, IEncryptionProvider? encryptionProvider,
            NeoIniOptions? options = null, bool autoLoad = true, CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(provider, encryptionProvider, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> for the specified file path,
        /// with optional configuration options.
        /// </summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="encryptionType">The encryption type to use for the file.</param>
        /// <param name="options">
        /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
        /// </param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation,
        /// containing the initialized <see cref="NeoIniDocument"/>.
        /// </returns>
        public static async Task<NeoIniDocument> CreateAsync(string? path, EncryptionType encryptionType = EncryptionType.None, NeoIniOptions? options = null,
            bool autoLoad = true, CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(path, encryptionType, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> for the specified file path
        /// using a custom encryption password.
        /// </summary>
        /// <param name="path">The absolute or relative path to the INI file.</param>
        /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
        /// <param name="options">
        /// Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.
        /// </param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation,
        /// containing the initialized <see cref="NeoIniDocument"/>.
        /// </returns>
        public static async Task<NeoIniDocument> CreateAsync(string? path, string? encryptionPassword, NeoIniOptions? options = null, bool autoLoad = true,
            CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(path, encryptionPassword, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> for the specified file path using a custom encryption provider.
        /// </summary>
        /// <param name="path">The absolute or relative path to the INI file.</param>
        /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting the file.</param>
        /// <param name="options">Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation,
        /// containing the initialized <see cref="NeoIniDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionProvider"/> is <c>null</c>.</exception>
        public static async Task<NeoIniDocument> CreateAsync(string? path, IEncryptionProvider? encryptionProvider,
                NeoIniOptions? options = null, bool autoLoad = true, CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(path, encryptionProvider, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> for the specified file path using a custom encryption password and a custom encryption provider.
        /// </summary>
        /// <param name="path">The absolute or relative path to the INI file.</param>
        /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
        /// <param name="encryptionProvider">The custom encryption provider to use for encrypting/decrypting the file.</param>
        /// <param name="options">Optional document configuration; if <c>null</c>, <see cref="NeoIniOptions.Default"/> is used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous initialization.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation,
        /// containing the initialized <see cref="NeoIniDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionProvider"/> is <c>null</c>.</exception>
        public static async Task<NeoIniDocument> CreateAsync(string? path, string? encryptionPassword, IEncryptionProvider? encryptionProvider,
                NeoIniOptions? options = null, bool autoLoad = true, CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(path, encryptionPassword, encryptionProvider, options, false);
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Creates a new <see cref="NeoIniDocument"/> using a specified data provider and enables human mode, 
        /// allowing the configuration to be formatted for easy reading and manual editing by users.
        /// </summary>
        /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
        /// <param name="options">Optional document configuration; if null, default settings are used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded synchronously from the provider during creation.
        /// If <c>false</c>, you must call <see cref="Reload"/> explicitly on the returned document.
        /// </param>
        /// <returns>A newly created and initialized <see cref="NeoIniDocument"/> instance configured for human mode.</returns>
        /// <remarks>
        /// <b>Warning:</b> This is an experimental feature and its use is not recommended for production environments.<br/>
        /// Activating this mode automatically disables checksum validation to accommodate manual 
        /// modifications to the data source. This mode cannot be used concurrently with encryption.
        /// </remarks>
        public static NeoIniDocument CreateHumanMode(INeoIniProvider? provider, NeoIniOptions? options = null, bool autoLoad = true)
        {
            NeoIniDocument document = new NeoIniDocument(provider, options, false) { HumanMode = true };
            if (autoLoad) document.Load();
            return document;
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="NeoIniDocument"/> using a specified data provider and enables human mode, 
        /// allowing the configuration to be formatted for easy reading and manual editing by users.
        /// </summary>
        /// <param name="provider">The pluggable data provider responsible for underlying data operations.</param>
        /// <param name="options">Optional document configuration; if null, default settings are used.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the provider during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation. 
        /// The task result contains the newly created <see cref="NeoIniDocument"/> configured for human mode.
        /// </returns>
        /// <remarks>
        /// <b>Warning:</b> This is an experimental feature and its use is not recommended for production environments.<br/>
        /// Activating this mode automatically disables checksum validation to accommodate manual 
        /// modifications to the data source. This mode cannot be used concurrently with encryption.
        /// </remarks>
        public static async Task<NeoIniDocument> CreateHumanModeAsync(INeoIniProvider? provider, NeoIniOptions? options = null, bool autoLoad = true,
                CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(provider, options, false) { HumanMode = true };
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }

        /// <summary>
        /// Enables human mode, allowing the software user to manually edit the INI configuration.
        /// </summary>
        /// <param name="path">The file path to the INI configuration.</param>
        /// <param name="options">Optional settings to configure the new <see cref="NeoIniDocument"/>.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded synchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="Reload"/> explicitly on the returned document.
        /// </param>
        /// <returns>A newly created and initialized <see cref="NeoIniDocument"/> instance configured for human mode.</returns>
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
        public static NeoIniDocument CreateHumanMode(string? path, NeoIniOptions? options = null, bool autoLoad = true)
        {
            NeoIniDocument document = new NeoIniDocument(path, EncryptionType.None, options, false) { HumanMode = true };
            if (autoLoad) document.Load();
            return document;
        }

        /// <summary>
        /// Asynchronously enables human mode, allowing the software user to manually edit the INI configuration.
        /// </summary>
        /// <param name="path">The file path to the INI configuration.</param>
        /// <param name="options">Optional settings to configure the new <see cref="NeoIniDocument"/>.</param>
        /// <param name="autoLoad">
        /// If <c>true</c>, the configuration data is loaded asynchronously from the file during creation.
        /// If <c>false</c>, you must call <see cref="ReloadAsync"/> explicitly on the returned document.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created <see cref="NeoIniDocument"/>.</returns>
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
        public static async Task<NeoIniDocument> CreateHumanModeAsync(string? path, NeoIniOptions? options = null, bool autoLoad = true,
            CancellationToken cancellationToken = default)
        {
            NeoIniDocument document = new NeoIniDocument(path, EncryptionType.None, options, false) { HumanMode = true };
            if (autoLoad) await document.LoadAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }
    }
}
