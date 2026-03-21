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

---

### 3.0 — Constructor Rework & EncryptionType

| Version | Status | Description |
|---------|--------|-------------|
| 3.0 | ✅ Released | Переработаны конструкторы; добавлено перечисление `EncryptionType`. |

### 3.x — Future Directions (Planned / Under Consideration)

| Version | Status | Description |
|---------|--------|-------------|
| 3.1 | 🟡 Planned | **Улучшение IEncryptionProvider**: перенос логики шифрования из `NeoIniFileProvider`. |
| 3.2 | 🔵 Under consideration | **Поддержка потоковых провайдеров**: возможность чтения/записи больших конфигураций инкрементально, без загрузки всего набора данных в память. |
| 3.3 | 🔵 Under consideration | **Расширенный source generator**: поддержка вложенных объектов, коллекций и атрибутов валидации (например, `[Range]`, `[Required]`) в сгенерированном коде маппинга. |
| 3.4 | 🔵 Under consideration | **Memory‑mapped I/O**: опциональное использование memory‑mapped файлов для очень больших INI‑файлов, чтобы повысить производительность и снизить потребление памяти. |
| 3.5 | 🔵 Under consideration | **Пакетные операции**: методы типа `SetValuesAsync` для атомарного обновления нескольких ключей за одну операцию, снижая накладные расходы автосохранения. |
| 3.6 | 🔵 Under consideration | **Поддержка .NET Standard 2.0**: обеспечение работы на .NET Framework 4.6.2+ и других устаревших платформах путём обратного портирования async/await и других современных возможностей. |
