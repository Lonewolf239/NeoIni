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
}
```

- `GetEncryptionParameters` is called when creating a new encrypted file or reading an existing one (with auto‑mode). It receives an optional password and salt. If both are null, the provider should generate a random salt and a default password (e.g., based on the machine).
- `GetEncryptionPassword` is called by `GetEncryptionPassword()` to retrieve the password that corresponds to a given salt (e.g., for migration).

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