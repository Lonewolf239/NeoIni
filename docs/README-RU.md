[![Wiki](https://img.shields.io/badge/NeoIni-WIKI-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni/wiki/Главная)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![Roadmap](https://img.shields.io/badge/ROADMAP-2D2D2D?style=for-the-badge&logo=map&logoColor=FFFFFF)](./ROADMAP-RU.md)
[![Changelog](https://img.shields.io/badge/CHANGELOG-2D2D2D?style=for-the-badge&logo=history&logoColor=FFFFFF)](./CHANGELOG-RU.md)

[![MIT](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=heart&logoColor=FFFFFF)](https://opensource.org/licenses/MIT)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](#thread-safe)
[![Downloads](https://img.shields.io/nuget/dt/NeoIni?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)

## Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# NeoIni

Безопасная, потокобезопасная библиотека конфигурации INI для .NET со встроенной проверкой целостности, AES-256 шифрованием и подключаемой архитектурой провайдеров.

```bash
dotnet add package NeoIni
```

- **Пакет:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Версия:** `2.0-pre1` | **.NET 6+**
- **Разработчик:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Feature | Details |
|---|---------|---------|
| 🔒 | **AES-256 encryption** | Прозрачное шифрование на уровне файла (CBC, IV + per-file salt). Ключ генерируется из окружения пользователя или задаётся вручную. |
| 🛡️ | <a name="thread-safe"></a> **SHA-256 checksum** | Проверка целостности при каждой загрузке/сохранении. При несовпадении — событие `ChecksumMismatch` + автоматический откат на `.backup`. |
| 🔐 | **Thread-safe** | `AsyncReaderWriterLock` защищает все операции чтения и записи при конкурентном доступе и полностью поддерживает `async`/`await`. |
| 📦 | **Typed Get/Set** | Чтение и запись `bool`, `int`, `double`, `DateTime`, `enum`, `string` и других типов с автоматическим парсингом и значениями по умолчанию. |
| ⚡ | **AutoSave & AutoBackup** | Автоматическое сохранение после N операций. Атомарная запись через `.tmp` + откат на `.backup` при ошибках. |
| 🔄 | **Hot-reload** | File watcher с поллингом и сравнением контрольных сумм — обновление конфига без перезапуска. |
| 🧩 | **Pluggable providers** | Интерфейс `INeoIniProvider` — храните конфиги в базе данных, удалённом сервисе, памяти или любом другом бэкенде. |
| 🗺️ | **Object mapping** | Source-generated `Get<T>()` / `Set<T>()` для POCO-классов через `NeoIniKeyAttribute`. |
| ✏️ | **Human-editable mode** | Сохранение комментариев и форматирования для ручного редактирования INI-файлов (без checksum, без шифрования). |
| 📡 | **Full async API** | Асинхронные версии всех основных операций — `CreateAsync`, `GetValueAsync`, `SaveFileAsync` и т.д. |
| 🔍 | **Search & TryGet** | Регистронезависимый поиск по ключам/значениям. `TryGetValue<T>` читает без модификации файла. |
| 📢 | **Rich event system** | 14 событий: сохранение, загрузка, CRUD ключей/секций, autosave, checksum mismatch, ошибки, завершение поиска. |
| 🔑 | **Easy migration** | Перенос зашифрованных конфигов между машинами через `GetEncryptionPassword()`. |
| 📦 | **Black-box design** | Единая точка входа — `NeoIniDocument` владеет и управляет всем за чистым публичным API. |

---

## Quick Start

### Creating an instance

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
using NeoIni;

// Без шифрования
NeoIniDocument document = new("config.ini");

// Авто-шифрование (привязка к машине)
NeoIniDocument encrypted = new("config.ini", autoEncryption: true);

// Пользовательский пароль (переносимый между машинами)
NeoIniDocument portable = new("config.ini", "MySecretPass123");

// Асинхронно
NeoIniDocument document = await NeoIniDocument.CreateAsync("config.ini", cancellationToken: ct);
```

</details>

### Reading & writing values

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
// Запись
document.SetValue("Database", "Host", "localhost");
document.SetValue("Database", "Port", 5432);

// Чтение с типизированными значениями по умолчанию
string host = document.GetValue("Database", "Host", "127.0.0.1");
int    port = document.GetValue("Database", "Port", 3306);

// Чтение без побочных эффектов (без AutoAdd, без модификации файла)
int level = document.TryGetValue("Game", "Level", 1);
```

```csharp
// Асинхронно
await document.SetValueAsync("Database", "Host", "localhost");
string host = await document.GetValueAsync("Database", "Host", "127.0.0.1", ct);
```

</details>

- Отсутствующие секции/ключи возвращают `defaultValue`; при включённом `UseAutoAdd` ключ создаётся автоматически.
- Поддерживаются `enum`, `DateTime` и любые `IConvertible` типы через invariant-culture парсинг.

### Section & key management

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
document.AddSection("Cache");
document.RemoveKey("Cache", "OldKey");
document.RenameSection("Cache", "AppCache");

string[] sections = document.GetAllSections();
string[] keys     = document.GetAllKeys("AppCache");
bool exists       = document.SectionExists("AppCache");
```

</details>

### Search

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
var results = document.Search("token");
foreach (var r in results)
    Console.WriteLine($"[{r.Section}] {r.Key} = {r.Value}");
```

</details>

### File operations

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
document.SaveFile();
document.ReloadFromFile();
document.DeleteFile();
document.DeleteFileWithData();
```

</details>

### Options & presets

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
document.UseAutoSave = true;
document.AutoSaveInterval = 3;    // сохранять каждые 3 записи
document.UseAutoBackup = true;
document.UseAutoAdd = true;
document.UseChecksum = true;
document.SaveOnDispose = true;
document.AllowEmptyValues = true;
```

</details>

Или используйте встроенные пресеты: `NeoIniOptions.Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(n)`.

### Events

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
document.Saved            += (_, _) => Console.WriteLine("Saved");
document.Loaded           += (_, _) => Console.WriteLine("Loaded");
document.KeyChanged       += (_, e) => Console.WriteLine($"[{e.Section}] {e.Key} → {e.Value}");
document.KeyAdded         += (_, e) => Console.WriteLine($"[{e.Section}] +{e.Key}");
document.ChecksumMismatch += (_, _) => Console.WriteLine("Checksum mismatch!");
document.Error            += (_, e) => Console.WriteLine($"Error: {e.Exception.Message}");
```

</details>

### Encryption & migration

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
// Авто-шифрование — ключ генерируется из user/machine/domain + per-file salt
NeoIniDocument document = new("secure.ini", autoEncryption: true);

// Получить пароль для миграции на другую машину
string password = document.GetEncryptionPassword();

// На новой машине
NeoIniDocument migrated = new("secure.ini", password);
```

</details>

### Disposal

<details>
  <summary>⚙️ Код: пример NeoIni</summary>

```csharp
using NeoIniDocument document = new("config.ini");
// SaveFile() вызывается автоматически, если SaveOnDispose = true
// После Dispose — ObjectDisposedException при любом обращении
```

</details>

---

## Advanced Features

- Attribute-based mapping & source generator (1.7+) — [подробный гайд](./ATTRIBUTE-MAPPING-RU.md)
- Hot-reload (1.7.1+) — [использование и нюансы](./HOT-RELOAD-RU.md)
- Human-editable INI mode (1.7.2+) — [экспериментальный режим](./HUMAN-MODE-RU.md)
- Pluggable provider abstraction (1.7.3+) — [кастомные провайдеры](./PROVIDERS-RU.md)
- Pluggable encryption (2.0+) — [кастомное шифрование](./ENCRYPTION-PROVIDER-RU.md)

---

## API Reference

Полная справка по методам, опциям и событиям — [API-RU.md](./API-RU.md)

---

## Philosophy

**Black Box Design** — вся внутренняя логика скрыта за простым публичным API класса `NeoIniDocument`. Вы работаете только с методами и событиями, не задумываясь о деталях реализации. INI-файлы NeoIni принадлежат библиотеке и управляются ей; комментарии в стандартном режиме намеренно не сохраняются (заголовок-предупреждение в файле сигнализирует об этом). Для ручного редактирования используйте [Human-editable mode](./HUMAN-MODE-RU.md).
