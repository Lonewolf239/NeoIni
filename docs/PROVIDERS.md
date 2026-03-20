[![EN](https://img.shields.io/badge/PROVIDERS-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./PROVIDERS.md)
[![RU](https://img.shields.io/badge/PROVIDERS-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./PROVIDERS-RU.md)

## Pluggable providers (1.7.3+)

Decouple configuration storage from the file system. The `INeoIniProvider` interface lets you back `NeoIniDocument` with a database, remote service, in-memory store, or any custom backend.

---

### Using a custom provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Synchronous
NeoIniDocument document = new(myCustomProvider);

// Asynchronous
NeoIniDocument document = await NeoIniDocument.CreateAsync(myCustomProvider, cancellationToken: ct);

// Human mode with a custom provider
NeoIniDocument document = NeoIniDocument.CreateHumanMode(myCustomProvider);
```

---

### Implementing INeoIniProvider

```csharp
using NeoIni.Models;
using NeoIni.Providers;

public class MyDatabaseProvider : INeoIniProvider
{
    public event EventHandler<ProviderErrorEventArgs> Error;
    public event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch;

    public NeoIniData GetData(bool humanization = false)
    {
        // Load data from your storage and return parsed sections + comments
    }

    public Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default)
    {
        // Async version of GetData
    }

    public void Save(string content, bool useChecksum)
    {
        // Persist the serialized INI content
    }

    public Task SaveAsync(string content, bool useChecksum, CancellationToken ct = default)
    {
        // Async version of Save
    }

    public byte[] GetStateChecksum()
    {
        // Return a hash of the current state for hot-reload detection,
        // or null if not supported
    }

    public void RaiseError(object sender, ProviderErrorEventArgs e)
        => Error?.Invoke(sender ?? this, e);
}
```

---

### Notes

- All existing file-based constructors (`new NeoIniDocument(path)`, encryption variants) continue to work — they use the built-in `NeoIniFileProvider` internally.
- `UseAutoBackup`, `DeleteFile`, `DeleteBackup`, and `GetEncryptionPassword` are file-provider-specific. Calling them on a custom provider throws `UnsupportedProviderOperationException`.
- Hot-reload works with any provider that returns a meaningful value from `GetStateChecksum()`.
