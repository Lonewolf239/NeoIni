## Hot Reload Support (1.7.1+)

Starting from **1.7.1**, NeoIni introduces a **hot‑reload mechanism** for automatically reloading configuration files as they change on disk — without restarting your application.

This feature is useful for long‑running services, debug sessions, or developer tools that want to respond dynamically to live configuration updates.

### Starting and stopping hot reload

You can start real‑time monitoring of the INI file by calling `StartHotReload(int delayMs)`.

This method checks the file for modifications at the specified interval (in milliseconds) and reloads it when a change is detected.

```csharp
NeoIniReader reader = new("config.ini");

// Start hot reload polling every 2 seconds
reader.StartHotReload(2000);

// Stop watching for modifications
reader.StopHotReload();
```

### Behavior and safety

- Minimum polling delay is **1000 ms**. An `InvalidHotReloadDelayException` will be thrown otherwise.  
- The hot reload process runs asynchronously in the background, monitoring file checksums.  
- Only one hot reload watcher can run at a time per instance.  
- On every detected change, NeoIni automatically calls `ReloadFromFileAsync()` to refresh configuration data in memory.

> **Note:** The feature incurs negligible overhead since it uses file checksum comparison and cancellation tokens for cooperative shutdown.
