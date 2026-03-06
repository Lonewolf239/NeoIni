# NeoIni

NeoIni is a fully-featured C# library for working with INI files that provides secure, thread-safe read/write configuration with built-in integrity checking (checksum) and optional AES encryption.

## Installation

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Version:** `1.6` | **.NET 6+**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- **Typed Get**: read values as `bool`, `int`, `double`, `DateTime`, `enum`, `string` and others with automatic parsing and defaults.
- **AutoAdd**: when reading via `GetValue<T>`, missing keys/sections can be automatically created with a default value.
- **Thread-safe**: uses `ReaderWriterLockSlim` for safe access from multiple threads.
- **AutoSave**: automatic saving after changes or at intervals (`AutoSave`, `AutoSaveInterval`).
- **AutoBackup**: creates a `.backup` file when saving to protect against corruption.
- **Checksum**: built-in SHA256 checksum validation to detect corruption/tampering.
- **Optional AES-256 encryption**: transparent file-level encryption with IV and per-file salt; key is derived from user environment or a custom password.
- **Full async API**: asynchronous versions for all major operations (`CreateAsync`, `GetValueAsync`, `SetKeyAsync`, `SaveFileAsync`, `AddSectionAsync`, etc.).
- **TryGet helpers**: `TryGetValue<T>` to read values **without** modifying the file or auto-creating keys.
- **Convenient API**: for managing sections and keys (create, rename, search, clear, delete).
- **Events**: hooks for saving, loading, key/section changes, autosave, errors, checksum mismatches, and search completion.
- **Easy migration**: transfer encrypted configs between machines via `GetEncryptionPassword()` when using auto-encryption.

## Security Features

- **Checksum (SHA256)**: when saving, a 32-byte checksum computed via SHA256 is appended to file contents; when reading it is verified, and if mismatched, you can handle the `OnChecksumMismatch` event or fall back to `.backup`.
- **AES-256**: when encryption is enabled, data is encrypted with AES in CBC mode using a 16-byte IV and a 32-byte key derived from a password (environment-based or custom) and a random 16-byte salt stored in the file.
- **Environment-based key**: in auto-encryption mode, the key is deterministically derived from `Environment.UserName`, `Environment.MachineName`, and `Environment.UserDomainName` plus a per-file salt, making the file unreadable on another host without a special password.
- **Backup fallback**: on read errors, checksum mismatch, or decryption errors, the library can automatically attempt to read the `.backup` file first.
- **Thread-safe access**: all read/write operations are wrapped in `ReaderWriterLockSlim`, preventing races under high load.

## Quick Start

### Creating a NeoIniReader Instance

#### Synchronous

```csharp
using NeoIni;

// No encryption
NeoIniReader reader = new("config.ini");

// Auto-encryption by environment (machine-bound)
NeoIniReader encryptedReader = new("config.ini", autoEncryption: true);

// Encryption with custom password (portable between machines)
NeoIniReader customEncrypted = new("config.ini", "MySecretPas123");
```

#### Asynchronous

```csharp
using NeoIni;
using var cts = new CancellationTokenSource();

NeoIniReader reader = await NeoIniReader.CreateAsync("config.ini", cancellationToken: cts.Token);
NeoIniReader encryptedReader = await NeoIniReader.CreateAsync("config.ini", autoEncryption: true, cancellationToken: cts.Token);
NeoIniReader customEncrypted = await NeoIniReader.CreateAsync("config.ini", "MySecretPas123", cancellationToken: cts.Token);
```

- When `autoEncryption = true`, the key is generated automatically and bound to the user/machine environment.
- When an `encryptionPassword` is provided, the key is derived from that password and a per-file salt, which is useful for transferring configs between machines.

### Reading Values

```csharp
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
DateTime when = reader.GetValue<DateTime>("Log", "LastRun", DateTime.Now);
```

Async:

```csharp
string text = await reader.GetValueAsync("Section1", "Key1", "default");
int number = await reader.GetValueAsync("Section1", "Number", 0);
bool flag = await reader.GetValueAsync("Section1", "Enabled", false);
double value = await reader.GetValueAsync("Section1", "Value", 0.0);
DateTime when = await reader.GetValueAsync("Log", "LastRun", DateTime.Now, cancellationToken);
```

- If a section/key is missing, `defaultValue` is returned; with `AutoAdd` enabled, the key may be automatically created in the file with that default.
- Reading `enum` and `DateTime` is supported via `Enum.TryParse`, `DateTime.TryParse`, and `Convert.ChangeType` (invariant culture).

### TryGet (without AutoAdd or file modification)

If you want pure read without auto-creation of keys and without touching the file, use `TryGetValue`:

```csharp
int level = reader.TryGetValue("Game", "Level", 1);
```

- These methods **never** write to the file and do not depend on `AutoAdd`: if the section or key does not exist, they simply return `defaultValue`.

### Writing Values

```csharp
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
reader.SetKey("Section1", "LastUpdate", DateTime.Now);
```

Async:

```csharp
await reader.SetKeyAsync("Section1", "Key1", "Value1");
await reader.SetKeyAsync("Section1", "Number", 42, cancellationToken);
```

- If a section/key doesn't exist, it will be created; changes trigger `OnKeyAdded` / `OnKeyChanged` and may trigger autosave.

### Example

```csharp
using NeoIni;

using NeoIniReader reader = new("config.ini");

// Initialize database settings
reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

// Read
string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"DB: {host}:{port}");
```

- On first run the file is created; on subsequent runs, values are read and reused/updated.

### Section/Key Management

```csharp
// Checks
bool sectionExists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");

// Create/delete
reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
reader.RemoveSection("Section1");

// Get lists
string[] sections = reader.GetAllSections();
string[] keys = reader.GetAllKeys("NewSection");

// Clear
reader.ClearSection("NewSection");
```

Async counterparts:

```csharp
await reader.AddSectionAsync("NewSection");
await reader.RemoveKeyAsync("Section1", "Key1");
await reader.RemoveSectionAsync("Section1", cancellationToken);
await reader.ClearSectionAsync("NewSection", cancellationToken);
```

- Section/key management methods are available in both sync and async variants where it makes sense.

### File Operations

```csharp
// Explicit save
reader.SaveFile();
await reader.SaveFileAsync();

// Reload data from disk
reader.ReloadFromFile();

// Delete file
reader.DeleteFile();         // file only
reader.DeleteFileWithData(); // file + clear Data
```

- When saving, an intermediate `.tmp` file is used, and if `AutoBackup` is enabled, a `.backup` is created and used as a fallback on read errors.

### Options

```csharp
reader.AutoSave = true;        // enable autosave
reader.AutoSaveInterval = 3;   // save every 3 write operations (if AutoSave is true)

reader.AutoBackup = true;      // enable .backup
reader.AutoAdd = true;         // auto-create keys on GetValue
reader.UseChecksum = true;     // enable checksum
reader.SaveOnDispose = true;   // save when Dispose() is called
```

You can also use presets via `NeoIniReaderOptions` when constructing the reader (e.g. `Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(interval)`).

### Events (Callbacks)

```csharp
reader.OnSave += () => Console.WriteLine("Saved");
reader.OnLoad += () => Console.WriteLine("Loaded");

reader.OnKeyChanged += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} changed to {value}");

reader.OnKeyAdded += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} added: {value}");

reader.OnKeyRemoved += (section, key) =>
    Console.WriteLine($"[{section}] {key} removed");

reader.OnSectionAdded += section =>
    Console.WriteLine($"Section added: {section}");

reader.OnSectionRemoved += section =>
    Console.WriteLine($"Section removed: {section}");

reader.OnSectionChanged += section =>
    Console.WriteLine($"Section changed: {section}");

reader.OnChecksumMismatch += (expected, actual) =>
    Console.WriteLine("Checksum mismatch detected!");

reader.OnAutoSave += () =>
    Console.WriteLine("AutoSave triggered");

reader.OnError += ex =>
    Console.WriteLine($"Error: {ex.Message}");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var (section, key, value) in results)
    Console.WriteLine($"[{section}] {key} = {value}");
```

- Search is performed on keys and values (case-insensitive); result is a list of tuples `(section, key, value)`. After search, `OnSearchCompleted` is called with the pattern and match count.

### Encryption & Migration

#### Auto-encryption (machine-bound)

```csharp
NeoIniReader reader = new("secure.ini", autoEncryption: true);
```

- The key is deterministically generated from the current user/machine/domain plus a random per-file salt. The file cannot be read on another machine without using the generated password.

To migrate to another machine, you can retrieve the password:

```csharp
string password = reader.GetEncryptionPassword();
// Save securely somewhere and use on the new machine
```

On the new machine:

```csharp
NeoIniReader migrated = new("secure.ini", password);
```

- If a custom password was used (`new NeoIniReader(path, "secret")`), `GetEncryptionPassword()` returns an informational status string and does **not** reveal the password itself.

### Disposal & Lifetime

```csharp
using NeoIniReader reader = new("config.ini");
// work with reader
// on leaving the using block:
//  - SaveFile() is called if SaveOnDispose is true
//  - Data is cleared and internal resources are freed
```

- After disposal any attempt to use the instance will throw `ObjectDisposedException`.

## API Reference

### Core methods

| Method | Description | Async Version |
|--------|-------------|---------------|
| `GetValue<T>` | Read typed value with default fallback (optionally auto-adding) | `GetValueAsync<T>` |
| `GetValueClamp<T>` | Read typed value and clamp it between min/max | `GetValueClampAsync<T>` |
| `TryGetValue<T>` | Read typed value without modifying the file and without AutoAdd | - |
| `SetKey<T>` | Set/create key-value | `SetKeyAsync<T>` |
| `AddSection` | Create section if missing | `AddSectionAsync` |
| `AddKeyInSection<T>` | Add unique key-value | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Delete specific key | `RemoveKeyAsync` |
| `RemoveSection` | Delete entire section | `RemoveSectionAsync` |
| `ClearSection` | Remove all keys from section | `ClearSectionAsync` |
| `RenameKey` | Rename key in section | `RenameKeyAsync` |
| `RenameSection` | Rename entire section | `RenameSectionAsync` |
| `Search` | Search keys/values by pattern | – |
| `FindKeyInAllSections` | Search a key across all sections | – |
| `GetAllSections` | List all sections | – |
| `GetAllKeys` | List keys in section | – |
| `GetSection` | Get all key-value pairs in section | – |
| `SectionExists` | Check if section exists | – |
| `KeyExists` | Check if key exists in section | – |
| `SaveFile` | Save data to a file | `SaveFileAsync` |
| `ToString` | Serialize INI data to formatted string (as in file) | – |
| `ReloadFromFile` | Reload data from file | – |
| `DeleteFile` | Delete file from disk | – |
| `DeleteFileWithData` | Delete file and clear data | – |
| `GetEncryptionPassword` | Get the encryption password (or status) | – |

### Options (NeoIniReaderOptions)

| Option | Description | Default |
|--------|-------------|---------|
| `AutoSave` | Automatically saves changes to disk after modifications | `true` |
| `AutoSaveInterval` | Number of operations between automatic saves when AutoSave is enabled | `0` (every change) |
| `AutoBackup` | Creates `.backup` files during save operations for safety | `true` |
| `AutoAdd` | Automatically creates missing sections/keys with default values when reading via `GetValue<T>` | `true` |
| `UseChecksum` | Calculates and verifies checksums during load/save operations | `true` |
| `SaveOnDispose` | Automatically saves the configuration when the instance is disposed | `true` |

### Events

| Action | Description |
|--------|-------------|
| `OnSave` | Called after saving a file to disk |
| `OnLoad` | Called after successfully loading data from a file or reloading |
| `OnKeyChanged` | Called when the value of an existing key in a section changes |
| `OnKeyAdded` | Called when a new key is added to a section |
| `OnKeyRemoved` | Called when a key is removed from a section |
| `OnSectionChanged` | Called whenever a section changes (keys are changed/added/removed) |
| `OnSectionAdded` | Called when a new section is added |
| `OnSectionRemoved` | Called when a section is deleted |
| `OnDataCleared` | Called when the data is completely cleared |
| `OnAutoSave` | Called before automatic saving |
| `OnChecksumMismatch` | Called when the checksum does not match while loading a file |
| `OnSearchCompleted` | Called after each search with the pattern and match count |
| `OnError` | Called when errors occur (parsing, saving, reading a file, etc.) |

## Philosophy

**Black Box Design**: all internal logic is hidden behind the simple public API of the `NeoIniReader` class. You work only with methods and events, without thinking about implementation details.
NeoIni config files are meant to be owned and managed by the library, not by humans editing them in Notepad — human comments are intentionally not preserved, and the warning header clearly signals this.
