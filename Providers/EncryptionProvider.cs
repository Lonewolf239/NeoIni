using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;

namespace NeoIni.Providers;

internal sealed class NeoIniEncryptionProvider : IEncryptionProvider
{
    private const int Pbkdf2Iterations = 320000;
    private const int KeySizeBytes = 32;

    private static byte[] DeriveKeyFromString(string? password, byte[]? salt, int keySize = KeySizeBytes)
    {
        ArgumentNullException.ThrowIfNull(password, nameof(password));
        ArgumentNullException.ThrowIfNull(salt, nameof(salt));
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, keySize);
    }

    private static byte[] GenerateRandomSalt(int size = 16)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Salt size must be positive.");
        var salt = new byte[size];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static string GeneratePasswordFromUserId(byte[]? salt)
    {
        salt ??= GenerateRandomSalt();
        string userId = Environment.UserName ?? Environment.GetEnvironmentVariable("USER") ?? "unknown";
        string envSeed = $"{userId}:{Environment.MachineName}:{Environment.UserDomainName ?? "local"}";
        byte[] passwordBytes = DeriveKeyFromString(envSeed, salt, KeySizeBytes);
        return Convert.ToHexString(passwordBytes).ToLowerInvariant();
    }

    public EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
    {
        salt ??= GenerateRandomSalt();
        password ??= GeneratePasswordFromUserId(salt);
        return new(DeriveKeyFromString(password, salt, KeySizeBytes), salt);
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
        using (CryptoStream cs = new(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
        }
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
        await memoryStream.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length), ct).ConfigureAwait(false);
        await memoryStream.WriteAsync(salt, ct).ConfigureAwait(false);
        using var encryptor = aes.CreateEncryptor();
        await using (var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length, ct).ConfigureAwait(false);
            await cs.FlushFinalBlockAsync(ct).ConfigureAwait(false);
        }
    }

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] encryptedBytes)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        using MemoryStream ms = new(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        using MemoryStream decryptedData = new();
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
        await cs.CopyToAsync(decryptedData, ct).ConfigureAwait(false);
        return decryptedData.ToArray();
    }
}
