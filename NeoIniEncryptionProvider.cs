using System;
using System.Security.Cryptography;
using System.Text;

namespace NeoIni;

internal sealed class NeoIniEncryptionProvider
{
    private static string GeneratePasswordFromUserId(int length = 16)
    {
        string userId = Environment.UserName ?? Environment.GetEnvironmentVariable("USER") ?? "unknown";
        string fullSeed = $"{userId}:{Environment.MachineName}:{Environment.UserDomainName ?? "local"}";
        byte[] salt = GenerateDeterministicSalt($"{userId}:{Environment.MachineName}");
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(fullSeed, salt, 320000, HashAlgorithmName.SHA256, 32);
        using var hmac = new HMACSHA256(key);
        byte[] finalSeed = hmac.ComputeHash(Encoding.UTF8.GetBytes(fullSeed + length));
        var password = new StringBuilder(length);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
        for (int i = 0; i < length; i++)
        {
            int index = (finalSeed[i % finalSeed.Length] + (i * 17)) % chars.Length;
            password.Append(chars[index]);
        }
        return password.ToString();
    }

    private static byte[] GenerateDeterministicSalt(string seed)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
        byte[] salt = new byte[16];
        Array.Copy(hash, salt, 16);
        return salt;
    }

    internal static byte[] GetEncryptionKey(string password = null)
    {
        if (password == null)
            return SHA256.HashData(Encoding.UTF8.GetBytes(GeneratePasswordFromUserId()))[..32];
        return SHA256.HashData(Encoding.UTF8.GetBytes(password))[..32];
    }

    internal static string GetEncryptionPassword() => GeneratePasswordFromUserId();
}
