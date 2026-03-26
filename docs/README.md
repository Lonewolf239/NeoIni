[![Wiki](https://img.shields.io/badge/NeoIni-WIKI-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni/wiki)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
[![Roadmap](https://img.shields.io/badge/ROADMAP-2D2D2D?style=for-the-badge&logo=map&logoColor=FFFFFF)](./ROADMAP.md)
[![Changelog](https://img.shields.io/badge/CHANGELOG-2D2D2D?style=for-the-badge&logo=history&logoColor=FFFFFF)](./CHANGELOG.md)

[![GPLv3](https://img.shields.io/badge/License-GPLv3-2D2D2D?style=for-the-badge&logo=gnu&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni/blob/main/LICENSE)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](#thread-safe)
[![Downloads](https://img.shields.io/nuget/dt/NeoIni?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)

### Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# NeoIni

Secure, thread-safe INI configuration library for .NET with built-in integrity checking, AES-256 encryption, and a pluggable provider architecture.

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Version:** 3.2.2 | **.NET 6+** | **.NET Standard 2.0**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Feature | Details |
|---|---------|---------|
| 🔒 | **AES-256 encryption** | Transparent file-level encryption (CBC, IV + per-file salt). Key derived from user environment or a custom password. |
| 🛡️ | <a name="thread-safe"></a> **SHA-256 checksum** | Integrity validation on every load/save. On mismatch — `ChecksumMismatch` event + automatic `.backup` fallback. |
| 🔐 | **Thread-safe** | `AsyncReaderWriterLock` protects all read/write operations under concurrent access with full `async`/`await` support. |
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
| 📦 | **Black-box design** | Single entrypoint — `NeoIniDocument` owns and manages everything behind a clean public API. |

---

## Quick Start

### Creating an instance

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
using NeoIni;

// Plain
NeoIniDocument document = new("config.ini");

// Auto-encryption (machine-bound)
NeoIniDocument encrypted = new("config.ini", EncryptionType.Auto);

// Custom password (portable between machines)
NeoIniDocument portable = new("config.ini", "MySecretPass123");

// Async
NeoIniDocument document = await NeoIniDocument.CreateAsync("config.ini", cancellationToken: ct);
```

</details>

### Reading & writing values

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
// Write
document.SetValue("Database", "Host", "localhost");
document.SetValue("Database", "Port", 5432);

// Read with typed defaults
string host = document.GetValue("Database", "Host", "127.0.0.1");
int    port = document.GetValue("Database", "Port", 3306);

// Read without side effects (no AutoAdd, no file modification)
int level = document.TryGetValue("Game", "Level", 1);
```

```csharp
// Async
await document.SetValueAsync("Database", "Host", "localhost");
string host = await document.GetValueAsync("Database", "Host", "127.0.0.1", ct);
```

</details>

- Missing sections/keys return `defaultValue`; with `UseAutoAdd` enabled the key is created automatically.
- Supports `enum`, `DateTime`, and any `IConvertible` type via invariant-culture parsing.

### Section & key management

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
document.AddSection("Cache");
document.RemoveKey("Cache", "OldKey");
document.RenameSection("Cache", "AppCache");

string[] sections = document.GetAllSections();
string[] keys     = document.GetAllKeys("AppCache");
bool exists       = document.SectionExists("AppCache");
```

</details>

### Search

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
var results = document.Search("token");
foreach (var r in results)
    Console.WriteLine($"[{r.Section}] {r.Key} = {r.Value}");
```

</details>

### File operations

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
document.SaveFile();
document.Reload();
document.DeleteFile();
document.DeleteFileWithData();
```

</details>

### Options & presets

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
document.UseAutoSave = true;
document.AutoSaveInterval = 3;    // save every 3 writes
document.UseAutoBackup = true;
document.UseAutoAdd = true;
document.UseChecksum = true;
document.SaveOnDispose = true;
document.AllowEmptyValues = true;
```

</details>

Or use built-in presets: `NeoIniOptions.Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(n)`.

### Events

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
document.Saved            += (_, _) => Console.WriteLine("Saved");
document.Loaded           += (_, _) => Console.WriteLine("Loaded");
document.KeyChanged       += (_, e) => Console.WriteLine($"[{e.Section}] {e.Key} → {e.Value}");
document.KeyAdded         += (_, e) => Console.WriteLine($"[{e.Section}] +{e.Key}");
document.ChecksumMismatch += (_, _) => Console.WriteLine("Checksum mismatch!");
document.Error            += (_, e) => Console.WriteLine($"Error: {e.Exception.Message}");
```

</details>

### Encryption & migration

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
// Auto-encryption — key is derived from user/machine/domain + per-file salt
NeoIniDocument document = new("secure.ini", EncryptionType.Auto);

// Retrieve password to migrate to another machine
string password = document.GetEncryptionPassword();

// On the new machine
NeoIniDocument migrated = new("secure.ini", password);
```

</details>

### Disposal

<details>
  <summary>⚙️ Code: NeoIni example</summary>

```csharp
using NeoIniDocument document = new("config.ini");
// SaveFile() is called automatically if SaveOnDispose is true
// After disposal — ObjectDisposedException on any access
```

</details>

---

## Advanced Features

- Attribute-based mapping & source generator (1.7+) — [detailed guide](./ATTRIBUTE-MAPPING.md)
- Hot-reload (1.7.1+) — [usage & caveats](./HOT-RELOAD.md)
- Human-editable INI mode (1.7.2+) — [experimental mode](./HUMAN-MODE.md)
- Pluggable provider abstraction (1.7.3+) — [custom providers](./PROVIDERS.md)
- Pluggable encryption (2.0+) — [custom encryption providers](./ENCRYPTION-PROVIDER.md)

---

## API Reference

Full method, options, and event reference — [API.md](./API.md)

---

## Philosophy

**Black Box Design** — all internal logic is hidden behind the simple public API of `NeoIniDocument`. You work only with methods and events, without thinking about implementation details. NeoIni config files are owned and managed by the library; human comments are intentionally not preserved in standard mode (the in-file warning header signals this). For hand-edited configs, use [Human-editable mode](./HUMAN-MODE.md).
