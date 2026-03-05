[![NeoIni](https://img.shields.io/badge/NeoIni-Black%20Box-2D2D2D?style=for-the-badge&logo=lock&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)

[![MIT](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=heart&logoColor=FFFFFF)](https://opensource.org/licenses/MIT)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![Downloads](https://img.shields.io/nuget/dt/NeoIni?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)

## Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# NeoIni

NeoIni is a fully-featured C# library for working with INI files that provides secure, thread-safe read/write configuration with built-in integrity checking (checksum) and optional AES encryption.

## Installation

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Version:** `1.5.8.1` | **.NET 6+**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- **Typed Get**: read values as `bool`, `int`, `double`, `DateTime`, `enum`, `string` and others with automatic parsing and defaults.
- **AutoAdd**: when reading via `GetValue<T>`, missing keys/sections can be automatically created with a default value.
- **Thread-safe**: uses `ReaderWriterLockSlim` for safe access from multiple threads.
- **AutoSave**: automatic saving after changes or at intervals (`UseAutoSaveInterval`, `AutoSaveInterval`).
- **AutoBackup**: creates a `.backup` file when saving to protect against corruption.
- **Checksum**: built-in SHA256 checksum validation to detect corruption/tampering.
- **Optional AES-256 encryption**: transparent file-level encryption with IV and key bound to user environment or custom password.
- **Full async API**: asynchronous versions for most operations (`GetValueAsync`, `SetKeyAsync`, `SaveFileAsync`, `ReloadFromFileAsync`, etc.).
- **Convenient API**: for managing sections and keys (create, rename, search, clear, delete).
- **Events**: hooks for saving, loading, key/section changes, autosave, errors, checksum mismatches, and search completion.
- **Easy migration**: transfer encrypted configs between machines via `GetEncryptionPassword()` with auto-encryption.

## Security Features

- **Checksum (SHA256)**: when saving, an 8-byte checksum computed via SHA256 is appended to file contents; when reading it is verified, and if mismatched, you can handle the `OnChecksumMismatch` event or fall back to `.backup`.
- **AES-256**: when encryption is enabled, data is encrypted with AES using a 16-byte IV and 32-byte key generated from user environment or a custom password.
- **Environment-based key**: by default, the key is deterministically generated from `Environment.UserName`, `Environment.MachineName`, and `Environment.UserDomainName`, making the file unreadable on another host without a special password.
- **Backup fallback**: on read errors, corruption, or decryption errors, the library can automatically attempt to read the `.backup` file.
- **Thread-safe access**: all read/write operations are wrapped in `ReaderWriterLockSlim`, preventing races under high load.

## Quick Start

### Creating a NeoIniReader Instance

```csharp
using NeoIni;

NeoIniReader reader = new("config.ini");                             // No encryption
NeoIniReader encryptedReader = new("config.ini", true);              // Auto-encryption by environment
NeoIniReader customEncrypted = new("config.ini", "MySecretPas123");  // Encryption with custom password
```

- When `autoEncryption = true`, the key is generated automatically and bound to the user/machine.
- When an `encryptionPassword` is provided, the key is deterministically derived from that password, which is useful for transferring configs between machines.

### Reading Values

```csharp
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
DateTime when = reader.GetValue<DateTime>("Log", "LastRun", DateTime.Now);
```

- If a section/key is missing, `defaultValue` is returned; with `AutoAdd` enabled, the key may be created in the file.
- Reading `enum` and `DateTime` is supported via `Convert.ChangeType`/`Enum.TryParse`/`DateTime.TryParse` (invariant culture).

### Writing Values

```csharp
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
reader.SetKey("Section1", "LastUpdate", DateTime.Now);
```

- If a section/key doesn't exist, it will be created; changes trigger `OnKeyAdded`/`OnKeyChanged` events if needed and may trigger autosave.

## Example

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

- In this example, on first run the file is created; on subsequent runs, values are simply read and updated.

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

- Section/key management methods are available in both sync and async variants (`AddSectionAsync`, `RemoveKeyAsync`, `RenameSectionAsync`, etc.).

### File Operations

```csharp
// Explicit save
reader.SaveFile();
await reader.SaveFileAsync();

// Reload data from disk
reader.ReloadFromFile();
await reader.ReloadFromFileAsync();

// Delete file
reader.DeleteFile();                // file only
reader.DeleteFileWithData();        // file + clear Data
```

- When saving, an intermediate `.tmp` file is used, and if `AutoBackup` is enabled, a `.backup` is created before atomic replacement.

### Options

```csharp
reader.AutoSave = true;             // enable autosave
reader.AutoSaveInterval = 3;        // save not every change, but every 3 operations

reader.AutoBackup = true;           // enable .backup
reader.AutoAdd = true;              // auto-create keys on GetValue
reader.UseChecksum = true;          // enable checksum
```

### Events (Callbacks)

```csharp
reader.OnSave += () => Console.WriteLine("Saved");
reader.OnLoad += () => Console.WriteLine("Loaded");

reader.OnKeyChanged += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} changed to {value}");

reader.OnKeyAdded += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} added: {value}");

reader.OnSectionAdded += section =>
    Console.WriteLine($"Section added: {section}");

reader.OnChecksumMismatch += (expected, actual) =>
    Console.WriteLine("Checksum mismatch detected!");

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

- The key is deterministically generated from the current user/machine/domain, so the file cannot be read on another machine without knowing the generated password.

To migrate to another machine, you can retrieve the password:

```csharp
string password = reader.GetEncryptionPassword();
// Save securely somewhere and use on the new machine
```

On the new machine:

```csharp
NeoIniReader migrated = new("secure.ini", password);
```

- If a custom password was used (`NeoIniReader(path, "secret")`), `GetEncryptionPassword()` will return a status without revealing the password.

### Disposal & Lifetime

```csharp
using NeoIniReader reader = new("config.ini");
// working with reader
// on exiting using block:
//  - SaveFile() is called
//  - data is cleared and resources are freed
```

- In `Dispose(bool)`, `SaveFile` is called (with exception suppression), internal `Data` is cleared, and `ReaderWriterLockSlim` is freed; after that the object is considered released.

## API Reference

| Method | Description | Async Version |
|--------|-----------  |---------------|
| `GetValue<T>` | Read typed value with default fallback | `GetValueAsync<T>` |
| `GetValueClamp<T>` | Read typed value and clamp it between min/max | `GetValueClampAsync<T>` |
| `SetKey<T>` | Set/create key-value | `SetKeyAsync<T>` |
| `AddSection` | Create section if missing | `AddSectionAsync` |
| `AddKeyInSection<T>` | Add unique key-value | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Delete specific key | `RemoveKeyAsync` |
| `RemoveSection` | Delete entire section | `RemoveSectionAsync` |
| `GetAllSections` | List all sections | - |
| `GetAllKeys` | List section keys | - |
| `GetSection` | Get all key-value pairs in section | - |
| `FindKeyInAllSections` | Search key across all sections | - |
| `ClearSection` | Remove all keys from section | `ClearSectionAsync` |
| `RenameKey` | Rename key in section | `RenameKeyAsync` |
| `RenameSection` | Rename entire section | `RenameSectionAsync` |
| `Search` | Search keys/values by pattern | - |
| `SectionExists` | Check if section exists | - |
| `KeyExists` | Check if key exists in section | - |
| `SaveFile` | Saving data to a file | `SaveFileAsync` |
| `ToString` | Serialize INI data to formatted string (as in file) | - |
| `ReloadFromFile` | Reloading data from a file | - |
| `DeleteFile` | Deleting a file from disk | - |
| `DeleteFileWithData` | Deleting a file and clearing data | - |
| `GetEncryptionPassword` | Getting the encryption password (or status) | - |

| Option | Description | Default |
|--------|-------------|---------|
| `AutoSave` | Automatically saves changes to disk after modifications | `true` |
| `AutoSaveInterval` | The number of operations between automatic save when AutoSave is enabled | `0` |
| `AutoBackup` | Creates backup files (.backup) during save operations for safety | `true` |
| `AutoAdd` | Automatically creates missing sections/keys with default values when reading via `GetValue<T>` | `true` |
| `UseChecksum` | Calculates and verifies checksums during load/save operations to detect corruption or tampering | `true` |
| `SaveOnDispose` | Automatically saves the configuration when the instance is disposed | `true` |

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
| `OnError` | Called when errors occur (parsing, saving, reading a file, etc.) |

## Philosophy

**Black Box Design**: all internal logic is hidden behind the simple public API of the `NeoIniReader` class. You work only with methods and events, without thinking about implementation details.
