## Roadmap · NeoIni 1.7.x

NeoIni 1.7.x focuses on bringing the library closer to enterprise-grade configuration providers while preserving the **Black Box** philosophy and the internal `Dictionary<string, Dictionary<string, string>>` data model.

| Version | Status | Description |
|---------|--------|-------------|
| 1.7 | ✅ Released | Object mapping API (`Get<T>`, `Set<T>`) via source generator. |
| 1.7.1 | ✅ Released | Hot-reload via file watcher (polling with checksum comparison). |
| 1.7.2 | ✅ Released | Human-editable INI mode (comment preservation, no checksum). |
| 1.7.3 | 🔧 In Progress | Pluggable provider abstraction (`INeoIniProvider`). |
| 1.8 | 🕓 Planned | Async-safe concurrency: replace `ReaderWriterLockSlim` with async-compatible primitive. |
| 1.9 | 🕓 Planned | Quoted value support: `key = "value ; not a comment"`. Broadens compatibility with real-world INI files (MySQL, PHP, Git config). |
| 2.0 | 🕓 Planned | Rename `NeoIniReader` → `NeoIniDocument`. Extract `HotReloadManager`. Eliminate sync/async code duplication. |
| 2.1 | 🕓 Planned | Reactive API: `reader.Observe<int>("Section", "Key")` → `IObservable<int>` for per-key change subscriptions. |
| 2.2 | 🕓 Planned | Hierarchical sections: `[Parent.Child]` navigation via `reader.GetSection("Parent").GetSection("Child")`. |
