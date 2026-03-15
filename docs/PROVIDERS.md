## Pluggable Providers (1.7.3+)

Starting with **1.7.3**, `NeoIniReader` works through the `INeoIniProvider` interface instead of being hardcoded to the file system. You can implement your own provider to store configuration in a database, remote service, in-memory store, or any other backend.

### Using a custom provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Synchronous
NeoIniReader reader = new(myCustomProvider);

// Asynchronous
NeoIniReader reader = await NeoIniReader.CreateAsync(myCustomProvider, cancellationToken: ct);

// Human mode with a custom provider
NeoIniReader reader = NeoIniReader.CreateHumanMode(myCustomProvider);
```

### Implementing INeoIniProvider

```csharp
using NeoIni.Internal;
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

### Notes

- All existing file-based constructors (`new NeoIniReader(path)`, encryption variants) continue to work exactly as before — they use the built-in `NeoIniFileProvider` internally.
- `UseAutoBackup`, `DeleteFile`, `DeleteBackup`, and `GetEncryptionPassword` are file-provider-specific. Calling them on a custom provider will throw `UnsupportedProviderOperationException`.
- Hot-reload works with any provider that returns a meaningful value from `GetStateChecksum()`.
