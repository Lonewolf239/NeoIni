[![EN](https://img.shields.io/badge/ROADMAP-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ROADMAP.md)
[![RU](https://img.shields.io/badge/ROADMAP-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ROADMAP-RU.md)

## Roadmap · NeoIni

Долгосрочный план развития NeoIni. Библиотека эволюционирует от однофайлового INI-ридера к универсальному конфигурационному фреймворку на основе провайдеров, сохраняя философию **Black Box** и внутреннюю модель данных `Dictionary<string, Dictionary<string, string>>`.

---

### 1.7.x — Foundation

| Version | Status | Description |
|--------|--------|----------|
| 1.7 | ✅ Released | API маппинга объектов (`Get<T>`, `Set<T>`) через source generator. |
| 1.7.1 | ✅ Released | Hot-reload через file watcher (поллинг с проверкой контрольной суммы). |
| 1.7.2 | ✅ Released | Режим ручного редактирования INI (сохранение комментариев, без контрольной суммы). |
| 1.7.3 | ✅ Released | Подключаемая абстракция провайдеров (`INeoIniProvider`). |

---

### 1.8 — Compatibility & Hardening

| Version | Status | Description |
|--------|--------|----------|
| 1.8 | ✅ Released | Поддержка значений в кавычках: `key = "value ; not a comment"`. Расширяет совместимость с реальными INI-файлами (MySQL, PHP, Git config). |

---

### 1.9 — Async Internals

| Version | Status | Description |
|--------|--------|----------|
| 1.9 | ✅ Released | Async-safe конкурентность: замена `ReaderWriterLockSlim` на async-совместимый примитив. |

---

### 2.0 — Major Redesign

| Version | Status | Description |
|--------|--------|----------|
| 2.0 | ✅ Released | Переименование `NeoIniReader` → `NeoIniDocument`. Введение интерфейса `IEncryptionProvider` для подключаемых алгоритмов шифрования (AES, пользовательские реализации). |
