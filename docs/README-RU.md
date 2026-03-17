[![NeoIni](https://img.shields.io/badge/NeoIni-Black%20Box-2D2D2D?style=for-the-badge&logo=lock&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![Roadmap](https://img.shields.io/badge/ROADMAP-2D2D2D?style=for-the-badge&logo=map&logoColor=FFFFFF)](./ROADMAP-RU.md)
[![Changelog](https://img.shields.io/badge/CHANGELOG-2D2D2D?style=for-the-badge&logo=history&logoColor=FFFFFF)](./CHANGELOG-RU.md)

[![MIT](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=heart&logoColor=FFFFFF)](https://opensource.org/licenses/MIT)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
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
- **Версия:** `1.9-pre1` | **.NET 6+**
- **Разработчик:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Feature | Details |
|---|---------|---------|
| 🔒 | **AES-256 encryption** | Прозрачное шифрование на уровне файла (CBC, IV + per-file salt). Ключ генерируется из окружения пользователя или задаётся вручную. |
| 🛡️ | **SHA-256 checksum** | Проверка целостности при каждой загрузке/сохранении. При несовпадении — событие `ChecksumMismatch` + автоматический откат на `.backup`. |
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
| 📦 | **Black-box design** | Единая точка входа — `NeoIniReader` владеет и управляет всем за чистым публичным API. |

---

## Quick Start

### Creating an instance

```csharp
using NeoIni;

// Без шифрования
NeoIniReader reader = new("config.ini");

// Авто-шифрование (привязка к машине)
NeoIniReader encrypted = new("config.ini", autoEncryption: true);

// Пользовательский пароль (переносимый между машинами)
NeoIniReader portable = new("config.ini", "MySecretPass123");

// Асинхронно
NeoIniReader reader = await NeoIniReader.CreateAsync("config.ini", cancellationToken: ct);
```

### Reading & writing values

```csharp
// Запись
reader.SetValue("Database", "Host", "localhost");
reader.SetValue("Database", "Port", 5432);

// Чтение с типизированными значениями по умолчанию
string host = reader.GetValue("Database", "Host", "127.0.0.1");
int    port = reader.GetValue("Database", "Port", 3306);

// Чтение без побочных эффектов (без AutoAdd, без модификации файла)
int level = reader.TryGetValue("Game", "Level", 1);
```

```csharp
// Асинхронно
await reader.SetValueAsync("Database", "Host", "localhost");
string host = await reader.GetValueAsync("Database", "Host", "127.0.0.1", ct);
```

- Отсутствующие секции/ключи возвращают `defaultValue`; при включённом `UseAutoAdd` ключ создаётся автоматически.
- Поддерживаются `enum`, `DateTime` и любые `IConvertible` типы через invariant-culture парсинг.

### Section & key management

```csharp
reader.AddSection("Cache");
reader.RemoveKey("Cache", "OldKey");
reader.RenameSection("Cache", "AppCache");

string[] sections = reader.GetAllSections();
string[] keys     = reader.GetAllKeys("AppCache");
bool exists       = reader.SectionExists("AppCache");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var r in results)
    Console.WriteLine($"[{r.Section}] {r.Key} = {r.Value}");
```

### File operations

```csharp
reader.SaveFile();
reader.ReloadFromFile();
reader.DeleteFile();
reader.DeleteFileWithData();
```

### Options & presets

```csharp
reader.UseAutoSave = true;
reader.AutoSaveInterval = 3;    // сохранять каждые 3 записи
reader.UseAutoBackup = true;
reader.UseAutoAdd = true;
reader.UseChecksum = true;
reader.SaveOnDispose = true;
reader.AllowEmptyValues = true;
```

Или используйте встроенные пресеты: `NeoIniReaderOptions.Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(n)`.

### Events

```csharp
reader.Saved            += (_, _) => Console.WriteLine("Saved");
reader.Loaded           += (_, _) => Console.WriteLine("Loaded");
reader.KeyChanged       += (_, e) => Console.WriteLine($"[{e.Section}] {e.Key} → {e.Value}");
reader.KeyAdded         += (_, e) => Console.WriteLine($"[{e.Section}] +{e.Key}");
reader.ChecksumMismatch += (_, _) => Console.WriteLine("Checksum mismatch!");
reader.Error            += (_, e) => Console.WriteLine($"Error: {e.Exception.Message}");
```

### Encryption & migration

```csharp
// Авто-шифрование — ключ генерируется из user/machine/domain + per-file salt
NeoIniReader reader = new("secure.ini", autoEncryption: true);

// Получить пароль для миграции на другую машину
string password = reader.GetEncryptionPassword();

// На новой машине
NeoIniReader migrated = new("secure.ini", password);
```

### Disposal

```csharp
using NeoIniReader reader = new("config.ini");
// SaveFile() вызывается автоматически, если SaveOnDispose = true
// После Dispose — ObjectDisposedException при любом обращении
```

---

## Advanced Features

- Attribute-based mapping & source generator (1.7+) — [подробный гайд](./ATTRIBUTE-MAPPING-RU.md)
- Hot-reload (1.7.1+) — [использование и нюансы](./HOT-RELOAD-RU.md)
- Human-editable INI mode (1.7.2+) — [экспериментальный режим](./HUMAN-MODE-RU.md)
- Pluggable provider abstraction (1.7.3+) — [кастомные провайдеры](./PROVIDERS-RU.md)

---

## API Reference

Полная справка по методам, опциям и событиям — [API-RU.md](./API-RU.md)

---

## Philosophy

**Black Box Design** — вся внутренняя логика скрыта за простым публичным API класса `NeoIniReader`. Вы работаете только с методами и событиями, не задумываясь о деталях реализации. INI-файлы NeoIni принадлежат библиотеке и управляются ей; комментарии в стандартном режиме намеренно не сохраняются (заголовок-предупреждение в файле сигнализирует об этом). Для ручного редактирования используйте [Human-editable mode](./HUMAN-MODE-RU.md).
