[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ROADMAP.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ROADMAP-RU.md)

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
| 1.9 | 🕓 Planned | Async-safe конкурентность: замена `ReaderWriterLockSlim` на async-совместимый примитив. |

---

### 2.0 — Major Redesign

| Version | Status | Description |
|--------|--------|----------|
| 2.0 | 🕓 Planned | Переименование `NeoIniReader` → `NeoIniDocument`. Выделение `HotReloadManager` в самостоятельный компонент. |

#### Стратегия миграции

| Step | Detail |
|------|--------|
| 2.0-beta | `NeoIniDocument` поставляется параллельно с `NeoIniReader`, помеченным `[Obsolete]`. Оба класса работают на одном внутреннем движке. |
| 2.0 | Удаление `NeoIniReader`. Однострочный гайд по миграции в release notes (`NeoIniReader` → `NeoIniDocument`, API не меняется кроме имени). |

---

### Cross-cutting (ongoing)

| Area | Description |
|---------|----------|
| Тесты | Поддерживать ≥ 90% покрытия строк. Интеграционные тесты для каждой реализации `INeoIniProvider`. Бенчмарк-сюит (BenchmarkDotNet) для отслеживания производительности парсинга, сохранения и hot-reload между релизами. |
| CI/CD | Пайплайн GitHub Actions: сборка → тесты → бенчмарки → публикация в NuGet. |
| Документация | XML-doc на каждом публичном члене. [README](./README-RU.md) с быстрым стартом, синхронизированный с последним релизом. Полный гайд по миграции при каждом мажорном обновлении. |
| Примеры | Проект `NeoIniDemo`, покрывающий: создание файла, секции, ключи/значения, clamp и auto-add, поиск и переименование, опции и пресеты, шифрование и миграцию паролей, асинхронные операции, авто-функции (auto-save, hot-reload), восстановление при ошибках файла, события, read-only и performance режимы, маппинг через атрибуты и source generator. |
