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
}
