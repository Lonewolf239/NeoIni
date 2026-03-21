using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NeoIni.Models;

namespace NeoIni.Providers;

/// <summary>Provides encryption parameters for use in encryption operations.</summary>
public interface IEncryptionProvider
{
    /// <summary>Gets encryption parameters based on an optional password and salt.</summary>
    /// <param name="password">The optional password used to derive the encryption key. If null, a default or generated key may be used.</param>
    /// <param name="salt">The optional salt used in key derivation. If null, a random salt may be generated.</param>
    /// <returns>An <see cref="EncryptionParameters"/> object containing the key and salt.</returns>
    EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null);

    /// <summary>Retrieves the encryption password derived from the provided salt.</summary>
    /// <param name="salt">The salt used to derive the password. May be null if the provider supports a default behavior.</param>
    /// <returns>The encryption password as a string.</returns>
    string GetEncryptionPassword(byte[]? salt);

    /// <summary>
    /// Encrypts plaintext bytes and writes the result to a memory stream.
    /// </summary>
    /// <param name="memoryStream">
    /// The stream to which the encrypted data will be written.
    /// The stream must be positioned at the start of the encryption block.
    /// After the method returns, the stream will contain:
    /// <list type="bullet">
    ///   <item><description>16-byte initialization vector (IV)</description></item>
    ///   <item><description>16-byte salt</description></item>
    ///   <item><description>the encrypted payload (size varies)</description></item>
    /// </list>
    /// This order is required for compatibility with the built-in file provider.
    /// </param>
    /// <param name="key">The encryption key (typically 32 bytes for AES-256).</param>
    /// <param name="salt">The salt used for key derivation (16 bytes).</param>
    /// <param name="plaintextBytes">The plaintext data to encrypt.</param>
    void Encrypt(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes);

    /// <summary>
    /// Asynchronously encrypts plaintext bytes and writes the result to a memory stream.
    /// </summary>
    /// <param name="memoryStream">
    /// The stream to which the encrypted data will be written.
    /// The stream must be positioned at the start of the encryption block.
    /// After the method returns, the stream will contain:
    /// <list type="bullet">
    ///   <item><description>16-byte initialization vector (IV)</description></item>
    ///   <item><description>16-byte salt</description></item>
    ///   <item><description>the encrypted payload (size varies)</description></item>
    /// </list>
    /// This order is required for compatibility with the built-in file provider.
    /// </param>
    /// <param name="key">The encryption key (typically 32 bytes for AES-256).</param>
    /// <param name="salt">The salt used for key derivation (16 bytes).</param>
    /// <param name="plaintextBytes">The plaintext data to encrypt.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EncryptAsync(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes, CancellationToken ct = default);

    /// <summary>
    /// Decrypts encrypted bytes using the specified key and initialization vector.
    /// </summary>
    /// <param name="key">The encryption key (same as used during encryption).</param>
    /// <param name="iv">The initialization vector (extracted from the encrypted file).</param>
    /// <param name="encryptedBytes">
    /// The encrypted data to decrypt. This should contain only the payload (IV and salt have already been removed by the caller).
    /// </param>
    /// <returns>The decrypted plaintext bytes.</returns>
    byte[] Decrypt(byte[] key, byte[] iv, byte[] encryptedBytes);

    /// <summary>
    /// Asynchronously decrypts encrypted bytes using the specified key and initialization vector.
    /// </summary>
    /// <param name="key">The encryption key (same as used during encryption).</param>
    /// <param name="iv">The initialization vector (extracted from the encrypted file).</param>
    /// <param name="encryptedBytes">
    /// The encrypted data to decrypt. This should contain only the payload (IV and salt have already been removed by the caller).
    /// </param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the decrypted plaintext bytes as the result.</returns>
    Task<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] encryptedBytes, CancellationToken ct = default);
}
