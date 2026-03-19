using System;
using System.Security.Cryptography;
using NeoIni.Models;

namespace NeoIni.Providers;

internal sealed class NeoIniEncryptionProvider
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

    internal static EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
    {
        salt ??= GenerateRandomSalt();
        password ??= GeneratePasswordFromUserId(salt);
        return new(DeriveKeyFromString(password, salt, KeySizeBytes), salt);
    }

    internal static string GetEncryptionPassword(byte[]? salt) => GeneratePasswordFromUserId(salt);
}
