using System;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;

namespace NeoIni.Providers;

/// <summary>
/// Contract for INI data storage providers.
/// Allows reading and saving configuration from any source.
/// </summary>
public interface INeoIniProvider
{
    /// <summary>Raised when an error occurs during reading, writing, parsing or cryptographic operations.</summary>
    event EventHandler<ProviderErrorEventArgs> Error;

    /// <summary>Raised when a data checksum mismatch is detected.</summary>
    event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch;

    /// <summary>Synchronously retrieves configuration data from the storage.</summary>
    /// <param name="humanization">If <c>true</c>, enables human-editable mode (relaxed validation, comment preservation).</param>
    /// <returns>A <see cref="NeoIniData"/> object containing the parsed data and comments.</returns>
    NeoIniData GetData(bool humanization = false);

    /// <summary>Asynchronously retrieves configuration data from the storage.</summary>
    /// <param name="humanization">If <c>true</c>, enables human-editable mode (relaxed validation, comment preservation).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task whose result is a <see cref="NeoIniData"/> object containing the parsed data and comments.</returns>
    Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default);

    /// <summary>Synchronously saves serialized configuration to the storage.</summary>
    /// <param name="content">The INI content as a string.</param>
    /// <param name="useChecksum">Whether to include a checksum for data integrity verification.</param>
    void Save(string content, bool useChecksum);

    /// <summary>Asynchronously saves serialized configuration to the storage.</summary>
    /// <param name="content">The INI content as a string.</param>
    /// <param name="useChecksum">Whether to include a checksum for data integrity verification.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(string content, bool useChecksum, CancellationToken ct = default);

    /// <summary>
    /// Returns a hash/checksum of the current storage state.
    /// Used by the hot-reload mechanism to detect external changes.
    /// </summary>
    /// <returns>A byte array containing the hash, or <c>null</c> if the provider does not support this feature.</returns>
    byte[] GetStateChecksum();

    /// <summary>Raises the <see cref="Error"/> event with the specified sender and error details.</summary>
    /// <param name="sender">The source of the error, or <c>null</c> to use the provider itself.</param>
    /// <param name="e">The event arguments containing the exception.</param>
    void RaiseError(object sender, ProviderErrorEventArgs e);
}
