# NeoIni

Secure, thread-safe INI configuration library for .NET with built-in integrity checking, AES-256 encryption, and a pluggable provider architecture.

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Version:** `1.7.3` | **.NET 6+**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Feature | Details |
|---|---------|---------|
| 🔒 | **AES-256 encryption** | Transparent file-level encryption (CBC, IV + per-file salt). Key derived from user environment or a custom password. |
| 🛡️ | **SHA-256 checksum** | Integrity validation on every load/save. On mismatch — `ChecksumMismatch` event + automatic `.backup` fallback. |
| 🔐 | **Thread-safe** | `ReaderWriterLockSlim` protects all read/write operations under concurrent access. |
| 📦 | **Typed Get/Set** | Read and write `bool`, `int`, `double`, `DateTime`, `enum`, `string` and more with automatic parsing and defaults. |
| ⚡ | **AutoSave & AutoBackup** | Automatic saving after N operations. Atomic writes via `.tmp` + `.backup` fallback on errors. |
| 🔄 | **Hot-reload** | File watcher with polling and checksum comparison — live config updates without restart. |
| 🧩 | **Pluggable providers** | `INeoIniProvider` interface — store configs in a database, remote service, memory, or any custom backend. |
| 🗺️ | **Object mapping** | Source-generated `Get<T>()` / `Set<T>()` for POCO classes via `NeoIniKeyAttribute`. |
| ✏️ | **Human-editable mode** | Preserve comments and formatting for hand-edited INI files (no checksum, no encryption). |
| 📡 | **Full async API** | Async versions for all major operations — `CreateAsync`, `GetValueAsync`, `SaveFileAsync`, etc. |
| 🔍 | **Search & TryGet** | Case-insensitive search across keys/values. `TryGetValue<T>` reads without modifying the file. |
| 📢 | **Rich event system** | 14 events: save, load, key/section CRUD, autosave, checksum mismatch, errors, search completion. |
| 🔑 | **Easy migration** | Transfer encrypted configs between machines via `GetEncryptionPassword()`. |
| 📦 | **Black-box design** | Single entrypoint — `NeoIniReader` owns and manages everything behind a clean public API. |

---

## Quick Start

### Creating an instance

```csharp
using NeoIni;

// Plain
NeoIniReader reader = new("config.ini");

// Auto-encryption (machine-bound)
NeoIniReader encrypted = new("config.ini", autoEncryption: true);

// Custom password (portable between machines)
NeoIniReader portable = new("config.ini", "MySecretPass123");

// Async
NeoIniReader reader = await NeoIniReader.CreateAsync("config.ini", cancellationToken: ct);
```

### Reading & writing values

```csharp
// Write
reader.SetValue("Database", "Host", "localhost");
reader.SetValue("Database", "Port", 5432);

// Read with typed defaults
string host = reader.GetValue("Database", "Host", "127.0.0.1");
int    port = reader.GetValue("Database", "Port", 3306);

// Read without side effects (no AutoAdd, no file modification)
int level = reader.TryGetValue("Game", "Level", 1);
```

```csharp
// Async
await reader.SetValueAsync("Database", "Host", "localhost");
string host = await reader.GetValueAsync("Database", "Host", "127.0.0.1", ct);
```

- Missing sections/keys return `defaultValue`; with `UseAutoAdd` enabled the key is created automatically.
- Supports `enum`, `DateTime`, and any `IConvertible` type via invariant-culture parsing.

### Section & key management

```csharp
reader.AddSection("Cache");
reader.RemoveKey("Cache", "OldKey");
reader.RenameSection("Cache", "AppCache");

string[] sections = reader.GetAllSections();
string[] keys     = reader.GetAllKeys("AppCache");
bool exists       = reader.SectionExists("AppCache");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var r in results)
    Console.WriteLine($"[{r.Section}] {r.Key} = {r.Value}");
```

### File operations

```csharp
reader.SaveFile();
reader.ReloadFromFile();
reader.DeleteFile();
reader.DeleteFileWithData();
```

### Options & presets

```csharp
reader.UseAutoSave = true;
reader.AutoSaveInterval = 3;    // save every 3 writes
reader.UseAutoBackup = true;
reader.UseAutoAdd = true;
reader.UseChecksum = true;
reader.SaveOnDispose = true;
reader.AllowEmptyValues = true;
```

Or use built-in presets: `NeoIniReaderOptions.Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(n)`.

### Events

```csharp
reader.Saved            += (_, _) => Console.WriteLine("Saved");
reader.Loaded           += (_, _) => Console.WriteLine("Loaded");
reader.KeyChanged       += (_, e) => Console.WriteLine($"[{e.Section}] {e.Key} → {e.Value}");
reader.KeyAdded         += (_, e) => Console.WriteLine($"[{e.Section}] +{e.Key}");
reader.ChecksumMismatch += (_, _) => Console.WriteLine("Checksum mismatch!");
reader.Error            += (_, e) => Console.WriteLine($"Error: {e.Exception.Message}");
```

### Encryption & migration

```csharp
// Auto-encryption — key is derived from user/machine/domain + per-file salt
NeoIniReader reader = new("secure.ini", autoEncryption: true);

// Retrieve password to migrate to another machine
string password = reader.GetEncryptionPassword();

// On the new machine
NeoIniReader migrated = new("secure.ini", password);
```

### Disposal

```csharp
using NeoIniReader reader = new("config.ini");
// SaveFile() is called automatically if SaveOnDispose is true
// After disposal — ObjectDisposedException on any access
```

---

## Advanced Features

- Attribute-based mapping & source generator (1.7+) — [detailed guide](https://github.com/Lonewolf239/NeoIni/blob/main/docs/ATTRIBUTE-MAPPING.md)
- Hot-reload (1.7.1+) — [usage & caveats](https://github.com/Lonewolf239/NeoIni/blob/main/docs/HOT-RELOAD.md)
- Human-editable INI mode (1.7.2+) — [experimental mode](https://github.com/Lonewolf239/NeoIni/blob/main/docs/HUMAN-MODE.md)
- Pluggable provider abstraction (1.7.3+) — [custom providers](https://github.com/Lonewolf239/NeoIni/blob/main/docs/PROVIDERS.md)

---

## API Reference

Full method, options, and event reference — [API.md](https://github.com/Lonewolf239/NeoIni/blob/main/docs/API.md)

---

## Philosophy

**Black Box Design** — all internal logic is hidden behind the simple public API of `NeoIniReader`. You work only with methods and events, without thinking about implementation details. NeoIni config files are owned and managed by the library; human comments are intentionally not preserved in standard mode (the in-file warning header signals this). For hand-edited configs, use [Human-editable mode](https://github.com/Lonewolf239/NeoIni/blob/main/docs/HUMAN-MODE.md).

---

## Changelog

Full version history and release notes — (CHANGELOG.md)[https://github.com/Lonewolf239/NeoIni/blob/main/docs/CHANGELOG.md]
