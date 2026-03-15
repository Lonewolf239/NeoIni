[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./HOT-RELOAD.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./HOT-RELOAD-RU.md)

## Hot-reload (1.7.1+)

Automatically detect configuration changes and reload data without restarting your application. NeoIni uses polling with checksum comparison to pick up external modifications to the INI source.

---

### Starting and stopping

```csharp
using NeoIni;

NeoIniReader reader = new("config.ini");

// Start hot-reload polling every 2 seconds
reader.StartHotReload(2000);

// Stop watching
reader.StopHotReload();
```

`StartHotReload(int delayMs)` accepts the polling interval in milliseconds. Minimum is **1000 ms** — lower values throw `InvalidHotReloadDelayException`. Call `StopHotReload()` to disable at any time.

---

### Behavior and safety

- **Polling + checksum:** On each interval, NeoIni computes a checksum of the current source state and compares it to the last known value. If the checksum differs, the data is reloaded.
- **Thread-safe:** Reloads acquire the internal `ReaderWriterLockSlim` write lock, so concurrent reads are safe.
- **Events:** A successful reload fires the `Loaded` event. If the reload fails, the `Error` event fires and the previous data is retained.
- **AutoSave interaction:** If `UseAutoSave` is enabled, in-memory changes are saved before the reload comparison runs, preventing data loss.

---

### Provider support

Since **1.7.3**, hot-reload works with any `INeoIniProvider` that returns a meaningful value from `GetStateChecksum()`. The built-in `NeoIniFileProvider` uses the file's last-write timestamp and size as its checksum. Custom providers can return any byte array that changes when the underlying data changes.
