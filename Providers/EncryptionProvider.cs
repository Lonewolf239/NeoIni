using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;

namespace NeoIni.Providers
{
    internal sealed class NeoIniEncryptionProvider : IEncryptionProvider
    {
        private const int Pbkdf2Iterations = 320000;
        private const int KeySizeBytes = 32;

        private static byte[] DeriveKeyFromString(string? password, byte[]? salt, int keySize = KeySizeBytes)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            if (salt is null) throw new ArgumentNullException(nameof(salt));
#if NETSTANDARD2_0
            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations);
            return rfc2898.GetBytes(keySize);
#else
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, keySize);
#endif
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

        private static string GeneratePasswordFromUserId(byte[]? salt)
        {
            salt ??= GenerateRandomSalt();
            string userId = Environment.UserName ?? Environment.GetEnvironmentVariable("USER") ?? "unknown";
            string envSeed = $"{userId}:{Environment.MachineName}:{Environment.UserDomainName ?? "local"}";
            byte[] passwordBytes = DeriveKeyFromString(envSeed, salt, KeySizeBytes);
#if NETSTANDARD2_0
            return BitConverter.ToString(passwordBytes).Replace("-", "").ToLowerInvariant();
#else
            return Convert.ToHexString(passwordBytes).ToLowerInvariant();
#endif
        }

        public EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
        {
            salt ??= GenerateRandomSalt();
            password ??= GeneratePasswordFromUserId(salt);
            return new EncryptionParameters(DeriveKeyFromString(password, salt, KeySizeBytes), salt);
        }

        public string GetEncryptionPassword(byte[]? salt) => GeneratePasswordFromUserId(salt);

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

        public async Task EncryptAsync(MemoryStream memoryStream, byte[] key, byte[] salt,
            byte[] plaintextBytes, CancellationToken ct = default)
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
