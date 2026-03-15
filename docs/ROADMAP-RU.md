## Roadmap · NeoIni

Долгосрочный план развития NeoIni. Библиотека эволюционирует от файлового INI-ридера к универсальному провайдер-ориентированному конфигурационному фреймворку, сохраняя философию **Black Box** и внутреннюю модель данных `Dictionary<string, Dictionary<string, string>>`.

### 1.7.x — Foundation

| Version | Status | Description |
|---------|--------|-------------|
| 1.7 | ✅ Released | Object mapping API (`Get<T>`, `Set<T>`) через source generator. |
| 1.7.1 | ✅ Released | Hot-reload через file watcher (поллинг с проверкой контрольной суммы). |
| 1.7.2 | ✅ Released | Human-editable INI mode (сохранение комментариев, без контрольной суммы). |
| 1.7.3 | ✅ Released | Pluggable provider abstraction (`INeoIniProvider`). |

### 1.8–1.9 — Hardening

| Version | Status | Description |
|---------|--------|-------------|
| 1.8 | 🕓 Planned | Async-safe concurrency: замена `ReaderWriterLockSlim` на async-совместимый примитив. |
| 1.9 | 🕓 Planned | Поддержка quoted values: `key = "value ; not a comment"`. Расширяет совместимость с реальными INI-файлами (MySQL, PHP, Git config). |

### 2.0–2.2 — Major Redesign

| Version | Status | Description |
|---------|--------|-------------|
| 2.0 | 🕓 Planned | Переименование `NeoIniReader` → `NeoIniDocument`. Выделение `HotReloadManager`. Устранение дублирования sync/async кода. |
| 2.1 | 🕓 Planned | Reactive API: `reader.Observe<int>("Section", "Key")` → `IObservable<int>` для подписки на изменения отдельных ключей. |
| 2.2 | 🕓 Planned | Hierarchical sections: навигация `[Parent.Child]` через `reader.GetSection("Parent").GetSection("Child")`. |
