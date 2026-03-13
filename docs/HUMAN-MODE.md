## Human‑editable INI mode (1.7.2+)

Version **1.7.2** introduces **HumanMode**, designed for tools and editors that allow users to manually modify INI files while keeping comments and layout intact.

This mode is experimental and **should not be used in production**.

### Creating a human‑editable reader

You can open a file in human mode either synchronously or asynchronously:

```csharp
// Synchronous
NeoIniReader reader = NeoIniReader.CreateHumanMode("config.ini");

// Asynchronous
NeoIniReader readerAsync = await NeoIniReader.CreateHumanModeAsync("config.ini");
```

When human mode is enabled:

- Checksum validation (`UseChecksum`) is automatically **disabled**, allowing free manual editing.
- Comments and formatting are **preserved and re‑emitted** when saving.
- The mode **cannot be combined with encryption** for safety reasons.

> **Warning:** As this is an experimental feature, file structure and behavior may change in future releases.
