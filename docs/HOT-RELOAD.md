[![EN](https://img.shields.io/badge/HOT_RELOAD-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./HOT-RELOAD.md)
[![RU](https://img.shields.io/badge/HOT_RELOAD-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./HOT-RELOAD-RU.md)

## Hot-reload (2.0+)

Automatically detect configuration changes and reload data without restarting your application. NeoIni uses polling with checksum comparison to pick up external modifications to the INI source.

---

### Starting and stopping (built-in monitor)

```csharp
using NeoIni;

NeoIniDocument document = new("config.ini");

// Start hot-reload polling every 2 seconds (built‑in file monitor)
document.StartHotReload(2000);

// Stop watching
document.StopHotReload();
```

`StartHotReload(int pollingInterval)` accepts the polling interval in milliseconds. For the built‑in file monitor the minimum is **1000 ms** — lower values throw `InvalidHotReloadDelayException`. Call `StopHotReload()` to disable at any time.

---

### Custom hot‑reload monitors (2.0+)

Version 2.0 introduces the `IHotReloadMonitor` interface, allowing you to implement your own change‑detection logic (e.g., using `FileSystemWatcher`, database notifications, or message queues).

```csharp
public interface IHotReloadMonitor : IDisposable
{
    event EventHandler? ChangeDetected;
    void Start(int pollingInterval);
    void Pause();
    void Continue();
    Task ContinueAsync(CancellationToken cancellationToken);
    void Stop();
}
```

Pass your custom monitor as the second parameter to `StartHotReload`:

```csharp
document.StartHotReload(2000, new MyCustomMonitor());
```

The built‑in file monitor is used by default when you omit the parameter. Custom monitors can define their own semantics for the `pollingInterval` – the 1000 ms restriction applies **only to the default file monitor**.

---

### Behavior and safety

- **Polling + checksum:** On each interval, the monitor computes a checksum of the current source state and compares it to the last known value. If the checksum differs, the `ChangeDetected` event fires and the document automatically reloads.
- **Thread-safe:** Reloads acquire the internal `AsyncReaderWriterLock` write lock, so concurrent reads are safe.
- **Events:** A successful reload fires the `Loaded` event. If the reload fails, the `Error` event fires and the previous data is retained.
- **Pause during save:** The monitor is automatically paused while the document saves to avoid triggering a reload on its own write.

---

### Provider support

Hot‑reload works with any `INeoIniProvider` that returns a meaningful value from `GetStateChecksum()`. The built‑in `NeoIniFileProvider` uses the file’s last‑write timestamp and size as its checksum. Custom providers can return any byte array that changes when the underlying data changes.

> **Note:** In versions prior to 2.0, hot‑reload was built directly into `NeoIniReader` and did not support custom monitors. Upgrade to 2.0 to take advantage of the new pluggable architecture.
