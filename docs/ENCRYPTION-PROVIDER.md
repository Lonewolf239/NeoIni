[![EN](https://img.shields.io/badge/ENCRYPTION_PROVIDER-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ENCRYPTION-PROVIDER.md)
[![RU](https://img.shields.io/badge/ENCRYPTION_PROVIDER-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ENCRYPTION-PROVIDER-RU.md)

## Pluggable encryption (2.0+)

Decouple encryption logic from the configuration storage. The `IEncryptionProvider` interface allows you to plug in custom encryption algorithms (e.g., AES-GCM, custom XOR, hardware-backed keys) while keeping the same `NeoIniDocument` API.

---

### Using a custom encryption provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Pass your provider to one of the document constructors
NeoIniDocument doc = new("config.ini", myEncryptionProvider);

// Or use the async factory
NeoIniDocument doc = await NeoIniDocument.CreateAsync("config.ini", myEncryptionProvider, options: null, autoLoad: true);
```

The provider is used to derive encryption keys and salts every time the file is read or written.

---

### Implementing IEncryptionProvider

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;
using NeoIni.Providers;

public class MyAesGcmEncryptionProvider : IEncryptionProvider
{
    public EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
    {
        // Generate or derive a key (e.g., from password and salt using a KDF)
        // Return a new EncryptionParameters(byte[] key, byte[] salt)
    }

    public string GetEncryptionPassword(byte[]? salt)
    {
        // Return the password that would be used to derive the key for the given salt.
        // This is used by the GetEncryptionPassword() method of NeoIniDocument.
    }

    public void Encrypt(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes)
    {
        // Write the initialization vector (16 bytes) and salt (16 bytes) to the stream,
        // then encrypt and write the payload.
        // The order MUST be: IV (16) + Salt (16) + EncryptedData.
        // This order is required for compatibility with the built-in file provider.
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM; // example
        aes.Key = key;
        aes.GenerateIV();
        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        memoryStream.Write(salt, 0, salt.Length);
        using var encryptor = aes.CreateEncryptor();
        using (var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
        }
    }

    public async Task EncryptAsync(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes, CancellationToken ct = default)
    {
        // Async version – same ordering
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
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
        // Decrypt using the key and IV (salt is already used in key derivation, not needed here)
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
        aes.Key = key;
        aes.IV = iv;
        using var ms = new MemoryStream(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    public async Task<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] encryptedBytes, CancellationToken ct = default)
    {
        // Async version
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
        aes.Key = key;
        aes.IV = iv;
        ct.ThrowIfCancellationRequested();
        using var ms = new MemoryStream(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        await cs.CopyToAsync(result, ct).ConfigureAwait(false);
        return result.ToArray();
    }
}
```

- `GetEncryptionParameters` is called when creating a new encrypted file or reading an existing one (with auto‑mode). It receives an optional password and salt. If both are null, the provider should generate a random salt and a default password (e.g., based on the machine).
- `GetEncryptionPassword` is called by `GetEncryptionPassword()` to retrieve the password that corresponds to a given salt (e.g., for migration).
- `Encrypt`/`EncryptAsync` must write the IV and salt to the stream **exactly** as described. The file provider reads them back in the same order.
- `Decrypt`/`DecryptAsync` receive the extracted IV and the encrypted payload (without IV or salt). They should return the decrypted plaintext bytes.

> **Important:** The order of writing is **IV (16 bytes) + Salt (16 bytes) + EncryptedData**. This format is expected by `NeoIniFileProvider`. If you change it, your files will not be readable by the built-in provider (or vice‑versa). If you only use your own provider, you may use any format, but you must be consistent.


---

### Built-in provider

NeoIni ships with a default AES‑256‑CBC implementation (`NeoIniEncryptionProvider`). It is used automatically when you use constructors that do not specify a custom provider. It supports two modes:

- **Auto‑encryption:** key derived from the current user/machine environment (non‑portable).
- **Password‑based:** key derived from a user‑supplied string via PBKDF2 (320 000 iterations).

---

### Notes

- All file‑based constructors (`new NeoIniDocument(path)`, `autoEncryption`, `encryptionPassword`) internally use the built‑in provider. You only need a custom provider if you want to replace the encryption algorithm.
- The `salt` parameter in `GetEncryptionParameters` is optional. If your provider does not use a salt, you can ignore it.
- The returned `EncryptionParameters` must contain a valid key (non‑null) when encryption is enabled. If you return `null` key, the document will throw `MissingEncryptionKeyException`.
- The provider should be stateless and thread‑safe, as it may be called concurrently from different documents.

> **Note:** In versions prior to 2.0, encryption was hard‑coded to AES‑256‑CBC. Upgrade to 2.0 to take advantage of the new pluggable encryption architecture.
