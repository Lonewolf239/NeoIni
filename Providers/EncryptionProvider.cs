using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;

namespace NeoIni.Providers
{
    /// <summary>
    /// Provides encryption and decryption functionality using AES-CBC with PKCS7 padding.
    /// Keys are derived from a password using PBKDF2 with SHA-256, or from machine-bound secret material if no password is supplied.
    /// </summary>
    public sealed class NeoIniEncryptionProvider : IEncryptionProvider
    {
        private const int Pbkdf2Iterations = 320000;
        private const int KeySizeBytes = 32;

        private static byte[] DeriveKey(byte[] password, byte[] salt, int keySize = KeySizeBytes)
        {
#if NETSTANDARD2_0 || NET5_0
            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations);
            return rfc2898.GetBytes(keySize);
#else
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, keySize);
#endif
        }

        private static byte[] DeriveKeyFromString(string? password, byte[]? salt, int keySize = KeySizeBytes)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            if (salt is null) throw new ArgumentNullException(nameof(salt));
            return DeriveKey(Encoding.UTF8.GetBytes(password), salt, keySize);
        }

        private static byte[] GenerateRandomSalt(int size = 16)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Salt size must be positive.");
            var salt = new byte[size];
#if NETSTANDARD2_0
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
#else
            RandomNumberGenerator.Fill(salt);
#endif
            return salt;
        }

        private static string ToHex(byte[] bytes)
        {
#if NETSTANDARD2_0
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
#else
            return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
        }

        /// <summary>
        /// Generates a deterministic, high-entropy password bound to this machine, using a locally-persisted
        /// random secret (protected with DPAPI on Windows) mixed with a stable hardware/installation identifier.
        /// The result is not derivable from any publicly observable information (user name, machine name, etc.).
        /// </summary>
        private static string GenerateAutoPassword(byte[] salt)
        {
            byte[] secret = MachineIdentity.GetMachineSecret();
            byte[] machineId = MachineIdentity.GetMachineId();
            byte[] combined = new byte[secret.Length + machineId.Length];
            Buffer.BlockCopy(secret, 0, combined, 0, secret.Length);
            Buffer.BlockCopy(machineId, 0, combined, secret.Length, machineId.Length);
            return ToHex(DeriveKey(combined, salt));
        }

        /// <summary>
        /// Reproduces the pre-3.5 automatic key derivation (user name + machine name + domain), used exclusively
        /// to decrypt files created before the machine-bound secret scheme existed, so they can be migrated.
        /// </summary>
        private static string GenerateLegacyAutoPassword(byte[] salt)
        {
            string userId = Environment.UserName ?? Environment.GetEnvironmentVariable("USER") ?? "unknown";
            string envSeed = $"{userId}:{Environment.MachineName}:{Environment.UserDomainName ?? "local"}";
            return ToHex(DeriveKeyFromString(envSeed, salt, KeySizeBytes));
        }

        /// <summary>
        /// Obtains encryption parameters (key and salt) for use with AES encryption.
        /// If a password is supplied, it is used to derive the key; otherwise a machine-bound password is generated.
        /// If salt is not supplied, a random salt is generated.
        /// </summary>
        /// <param name="password">Optional password. If <c>null</c>, a password is generated from machine-bound secret material.</param>
        /// <param name="salt">Optional salt. If <c>null</c>, a random salt is generated.</param>
        /// <returns>An <see cref="EncryptionParameters"/> instance containing the derived key and the salt used.</returns>
        public EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
        {
            salt ??= GenerateRandomSalt();
            password ??= GenerateAutoPassword(salt);
            return new EncryptionParameters(DeriveKeyFromString(password, salt, KeySizeBytes), salt);
        }

        /// <summary>Retrieves the deterministic, machine-bound password derived for the provided salt.</summary>
        /// <param name="salt">The salt used in password derivation.</param>
        /// <returns>A hex string password.</returns>
        public string GetEncryptionPassword(byte[]? salt)
        {
            salt ??= GenerateRandomSalt();
            return GenerateAutoPassword(salt);
        }

        /// <summary>
        /// Obtains encryption parameters using the pre-3.5 automatic key derivation. Used only internally
        /// by <see cref="Providers.NeoIniFileProvider"/> to decrypt and migrate version-1 automatically-encrypted files.
        /// </summary>
        /// <param name="salt">The salt stored in the version-1 file.</param>
        internal EncryptionParameters GetLegacyAutoEncryptionParameters(byte[] salt) =>
            new EncryptionParameters(DeriveKeyFromString(GenerateLegacyAutoPassword(salt), salt, KeySizeBytes), salt);

        /// <summary>
        /// Encrypts plaintext bytes using AES-CBC with PKCS7 padding.
        /// The encryption output includes the AES initialization vector (IV) followed by the salt, then the ciphertext.
        /// </summary>
        /// <param name="memoryStream">The stream to which the encrypted data (IV + salt + ciphertext) will be written.</param>
        /// <param name="key">The encryption key (must be 32 bytes for AES-256).</param>
        /// <param name="salt">The salt used to derive the key (stored alongside the ciphertext for later decryption).</param>
        /// <param name="plaintextBytes">The plaintext data to encrypt.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is <c>null</c>.</exception>
        public void Encrypt(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();
            memoryStream.Write(aes.IV, 0, aes.IV.Length);
            memoryStream.Write(salt, 0, salt.Length);
            using var encryptor = aes.CreateEncryptor();
#if NETSTANDARD2_0
            using var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
#else
            using CryptoStream cs = new(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true);
#endif
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
        }

        /// <summary>
        /// Asynchronously encrypts plaintext bytes using AES-CBC with PKCS7 padding.
        /// The encryption output includes the AES initialization vector (IV) followed by the salt, then the ciphertext.
        /// </summary>
        /// <param name="memoryStream">The stream to which the encrypted data (IV + salt + ciphertext) will be written.</param>
        /// <param name="key">The encryption key (must be 32 bytes for AES-256).</param>
        /// <param name="salt">The salt used to derive the key (stored alongside the ciphertext for later decryption).</param>
        /// <param name="plaintextBytes">The plaintext data to encrypt.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is canceled.</exception>
        public async Task EncryptAsync(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes, CancellationToken ct = default)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();
            ct.ThrowIfCancellationRequested();
#if NETSTANDARD2_0
            memoryStream.Write(aes.IV, 0, aes.IV.Length);
            memoryStream.Write(salt, 0, salt.Length);
            using var encryptor = aes.CreateEncryptor();
            using var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
#else
            await memoryStream.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length), ct).ConfigureAwait(false);
            await memoryStream.WriteAsync(salt, ct).ConfigureAwait(false);
            using var encryptor = aes.CreateEncryptor();
            await using var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true);
            await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length, ct).ConfigureAwait(false);
            await cs.FlushFinalBlockAsync(ct).ConfigureAwait(false);
#endif
        }

        /// <summary>Decrypts ciphertext using the provided key and initialization vector.</summary>
        /// <param name="key">The decryption key (must be 32 bytes for AES-256).</param>
        /// <param name="iv">The initialization vector (IV) used during encryption.</param>
        /// <param name="encryptedBytes">The ciphertext bytes to decrypt.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="CryptographicException">Thrown if decryption fails due to invalid key, padding, or corrupted data.</exception>
        public byte[] Decrypt(byte[] key, byte[] iv, byte[] encryptedBytes)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream(encryptedBytes);
            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var decryptedData = new MemoryStream();
            cs.CopyTo(decryptedData);
            return decryptedData.ToArray();
        }

        /// <summary>Asynchronously decrypts ciphertext using the provided key and initialization vector.</summary>
        /// <param name="key">The decryption key (must be 32 bytes for AES-256).</param>
        /// <param name="iv">The initialization vector (IV) used during encryption.</param>
        /// <param name="encryptedBytes">The ciphertext bytes to decrypt.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with the decrypted plaintext bytes as the result.</returns>
        /// <exception cref="CryptographicException">Thrown if decryption fails due to invalid key, padding, or corrupted data.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is canceled.</exception>
        public async Task<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] encryptedBytes, CancellationToken ct = default)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            ct.ThrowIfCancellationRequested();
            using var ms = new MemoryStream(encryptedBytes);
            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var decryptedData = new MemoryStream();
#if NETSTANDARD2_0
            cs.CopyTo(decryptedData);
#else
            await cs.CopyToAsync(decryptedData, ct).ConfigureAwait(false);
#endif
            return decryptedData.ToArray();
        }
    }
}
