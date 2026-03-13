[![NeoIni](https://img.shields.io/badge/NeoIni-Black%20Box-2D2D2D?style=for-the-badge&logo=lock&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![Roadmap](https://img.shields.io/badge/ROADMAP-2D2D2D?style=for-the-badge&logo=map&logoColor=FFFFFF)](./ROADMAP.md)

[![MIT](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=heart&logoColor=FFFFFF)](https://opensource.org/licenses/MIT)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![Downloads](https://img.shields.io/nuget/dt/NeoIni?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)

## Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# NeoIni

NeoIni — это полнофункциональная библиотека C# для работы с INI-файлами, которая обеспечивает безопасное, поточно-ориентированное (thread-safe) чтение/запись конфигурации со встроенной проверкой целостности (контрольная сумма) и опциональным AES шифрованием.

## Installation

```bash
dotnet add package NeoIni
```

- **Пакет:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Версия:** `1.7.2` | **.NET 6+**
- **Разработчик:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- **Типизированный Get**: чтение значений как `bool`, `int`, `double`, `DateTime`, `enum`, `string` и других с автоматическим парсингом и значениями по умолчанию.
- **AutoAdd**: при чтении через `GetValue<T>` отсутствующие ключи/секции могут автоматически создаваться со значением по умолчанию.
- **Потокобезопасность (Thread-safe)**: использует `ReaderWriterLockSlim` для безопасного доступа из нескольких потоков.
- **AutoSave**: автоматическое сохранение после изменений или через заданные интервалы (`UseAutoSave`, `AutoSaveInterval`).
- **AutoBackup**: создает файл `.backup` при сохранении для защиты от повреждения данных.
- **Контрольная сумма (Checksum)**: встроенная проверка контрольной суммы SHA256 для обнаружения повреждений/вмешательства.
- **Опциональное AES-256 шифрование**: прозрачное шифрование на уровне файла с использованием вектора инициализации (IV) и уникальной соли (salt) для каждого файла; ключ генерируется на основе окружения пользователя или из заданного пароля.
- **Полный асинхронный API**: асинхронные версии для всех основных операций (`CreateAsync`, `GetValueAsync`, `SetValueAsync`, `SaveFileAsync`, `AddSectionAsync` и т.д.).
- **Вспомогательные методы TryGet**: `TryGetValue<T>` для чтения значений **без** изменения файла или автоматического создания ключей.
- **Удобный API**: для управления секциями и ключами (создание, переименование, поиск, очистка, удаление).
- **События (Events)**: хуки для сохранения, загрузки, изменения ключей/секций, автосохранения, ошибок, несовпадения контрольной суммы и завершения поиска.
- **Простая миграция**: перенос зашифрованных конфигураций между машинами с помощью `GetEncryptionPassword()` при использовании автошифрования.
- **Связывание на основе атрибутов (1.7+)**: добавляйте атрибут **NeoIniKeyAttribute** к свойствам классов конфигурации (POCO) и используйте сгенерированные методы `NeoIniReader.Get<T>()` / `NeoIniReader.Set<T>()`, чтобы считать или записать всю конфигурацию одним вызовом.  
- **«Чёрный ящик»‑дизайн**: единственная точка входа — **NeoIniReader**, который владеет и управляет содержимым INI‑файла.

## Security Features

- **Контрольная сумма (SHA256)**: при сохранении 32-байтовая контрольная сумма, вычисленная с помощью SHA256, добавляется в конец содержимого файла; при чтении она проверяется, и в случае несовпадения вы можете обработать событие `ChecksumMismatch` или библиотека переключится на `.backup`.
- **AES-256**: когда шифрование включено, данные шифруются алгоритмом AES в режиме CBC с использованием 16-байтового вектора инициализации (IV) и 32-байтового ключа. Ключ создается на основе пароля (взятого из окружения или заданного пользователем) и случайной 16-байтовой соли, хранящейся в файле.
- **Ключ на основе окружения**: в режиме автошифрования ключ детерминированно генерируется из `Environment.UserName`, `Environment.MachineName` и `Environment.UserDomainName` плюс соли конкретного файла, что делает файл нечитаемым на другом хосте без специального пароля.
- **Резервный вариант (Backup fallback)**: при ошибках чтения, несовпадении контрольной суммы или ошибках расшифровки библиотека может автоматически попытаться сначала прочитать файл `.backup`.
- **Потокобезопасный доступ**: все операции чтения/записи обернуты в `ReaderWriterLockSlim`, что предотвращает состояние гонки (race conditions) при высокой нагрузке.

## Quick Start

### Creating a NeoIniReader Instance

#### Synchronous

```csharp
using NeoIni;

// Без шифрования
NeoIniReader reader = new("config.ini");

// Автошифрование через окружение (привязано к машине)
NeoIniReader encryptedReader = new("config.ini", autoEncryption: true);

// Шифрование с пользовательским паролем (переносимо между машинами)
NeoIniReader customEncrypted = new("config.ini", "MySecretPas123");
```

#### Asynchronous

```csharp
using NeoIni;
using var cts = new CancellationTokenSource();

NeoIniReader reader = await NeoIniReader.CreateAsync("config.ini", cancellationToken: cts.Token);
NeoIniReader encryptedReader = await NeoIniReader.CreateAsync("config.ini", autoEncryption: true, cancellationToken: cts.Token);
NeoIniReader customEncrypted = await NeoIniReader.CreateAsync("config.ini", "MySecretPas123", cancellationToken: cts.Token);
```

- Когда `autoEncryption = true`, ключ генерируется автоматически и привязывается к окружению пользователя/машины.
- Когда предоставляется `encryptionPassword`, ключ создается на основе этого пароля и соли файла, что полезно для переноса конфигов между машинами.

### Reading Values

```csharp
string text = reader.GetValue("Section1", "Key1", "default");
int number = reader.GetValue("Section1", "Number", 0);
bool flag = reader.GetValue("Section1", "Enabled", false);
double value = reader.GetValue("Section1", "Value", 0.0);
DateTime when = reader.GetValue("Log", "LastRun", DateTime.Now);
```

Асинхронно (Async):

```csharp
string text = await reader.GetValueAsync("Section1", "Key1", "default", cancellationToken);
int number = await reader.GetValueAsync("Section1", "Number", 0);
bool flag = await reader.GetValueAsync("Section1", "Enabled", false, cancellationToken);
double value = await reader.GetValueAsync("Section1", "Value", 0.0);
DateTime when = await reader.GetValueAsync("Log", "LastRun", DateTime.Now, cancellationToken);
```

- Если секция/ключ отсутствует, возвращается `defaultValue`; при включенном `UseAutoAdd` ключ может быть автоматически создан в файле с этим дефолтным значением.
- Чтение `enum` и `DateTime` поддерживается через `Enum.TryParse`, `DateTime.TryParse` и `Convert.ChangeType` (инвариантная культура).

### TryGet (without AutoAdd or file modification)

Если вы хотите выполнить чистое чтение без автоматического создания ключей и без изменения файла, используйте `TryGetValue`:

```csharp
int level = reader.TryGetValue("Game", "Level", 1);
```

- Эти методы **никогда** не пишут в файл и не зависят от `UseAutoAdd`: если секция или ключ не существуют, они просто возвращают `defaultValue`.

### Writing Values

```csharp
reader.SetValue("Section1", "Key1", "Value1");
reader.SetValue("Section1", "Number", 42);
reader.SetValue("Section1", "Enabled", true);
reader.SetValue("Section1", "LastUpdate", DateTime.Now);
```

Асинхронно (Async):

```csharp
await reader.SetValueAsync("Section1", "Key1", "Value1");
await reader.SetValueAsync("Section1", "Number", 42, cancellationToken);
```

- Если секция/ключ не существуют, они будут созданы; изменения вызывают события `KeyAdded` / `KeyChanged` и могут запустить автосохранение.

### Example

```csharp
using NeoIni;

using NeoIniReader reader = new("config.ini");

// Инициализация настроек базы данных
reader.SetValue("Database", "Host", "localhost");
reader.SetValue("Database", "Port", 5432);
reader.SetValue("Settings", "AutoSave", true);

// Чтение
string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"DB: {host}:{port}");
```

- При первом запуске файл создается; при последующих запусках значения считываются и переиспользуются/обновляются.

### Section/Key Management

```csharp
// Проверки
bool sectionExists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");

// Создание/удаление
reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
reader.RemoveSection("Section1");

// Получение списков
string[] sections = reader.GetAllSections();
string[] keys = reader.GetAllKeys("NewSection");

// Очистка
reader.ClearSection("NewSection");
```

Асинхронные аналоги:

```csharp
await reader.AddSectionAsync("NewSection");
await reader.RemoveKeyAsync("Section1", "Key1");
await reader.RemoveSectionAsync("Section1", cancellationToken);
await reader.ClearSectionAsync("NewSection", cancellationToken);
```

- Методы управления секциями/ключами доступны как в синхронном, так и в асинхронном вариантах там, где это имеет смысл.

### File Operations

```csharp
// Явное сохранение
reader.SaveFile();
await reader.SaveFileAsync();

// Перезагрузка данных с диска
reader.ReloadFromFile();

// Удаление файла
reader.DeleteFile();          // только файл
reader.DeleteFileWithData();  // файл + очистка данных из памяти
```

- При сохранении используется промежуточный файл `.tmp`, и если включен `UseAutoBackup`, создается файл `.backup`, который используется как запасной вариант при ошибках чтения.

### Options

```csharp
reader.UseAutoSave = true;       // включить автосохранение
reader.AutoSaveInterval = 3;     // сохранять каждые 3 операции записи (если AutoSave = true)

reader.UseAutoBackup = true;     // включить .backup
reader.UseAutoAdd = true;        // автосоздание ключей при GetValue
reader.UseChecksum = true;       // включить контрольную сумму
reader.SaveOnDispose = true;     // сохранять при вызове Dispose()
reader.AllowEmptyValues = true;  // разрешить пустые значения
```

Вы также можете использовать пресеты через `NeoIniReaderOptions` при создании ридера (например, `Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(interval)`).

### Events (Callbacks)

```csharp
reader.Saved += (_, _) => Console.WriteLine("Saved");
reader.Loaded += (_, _) => Console.WriteLine("Loaded");

reader.KeyChanged += (_, e) =>
    Console.WriteLine($"[{e.Section}] {e.Key} changed to {e.Value}");

reader.KeyAdded += (_, e) =>
    Console.WriteLine($"[{e.Section}] {e.Key} added: {e.Value}");

reader.KeyRemoved += (_, e) =>
    Console.WriteLine($"[{section}] {key} removed");

reader.SectionAdded += (_, e) =>
    Console.WriteLine($"Section added: {e.Section}");

reader.SectionRemoved += (_, e) =>
    Console.WriteLine($"Section removed: {e.Section}");

reader.SectionChanged += (_, e) =>
    Console.WriteLine($"Section changed: {e.Section}");

reader.ChecksumMismatch += (_, _) =>
    Console.WriteLine("Checksum mismatch detected!");

reader.AutoSave += (_, _) =>
    Console.WriteLine("AutoSave triggered");

reader.Error += (_, e) =>
    Console.WriteLine($"Error: {e.Exception.Message}");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var item in results)
    Console.WriteLine($"[{item.Section}] {item.Key} = {item.Value}");
```

- Поиск выполняется по ключам и значениям (без учета регистра); результат — список `SearchResult`. После поиска вызывается событие `SearchCompleted` с указанием паттерна и количества совпадений.

### Encryption & Migration

#### Auto-encryption (machine-bound)

```csharp
NeoIniReader reader = new("secure.ini", autoEncryption: true);
```

- Ключ детерминированно генерируется из текущего пользователя/машины/домена плюс случайная соль файла. Файл не может быть прочитан на другой машине без использования сгенерированного пароля.

Чтобы перенести конфиг на другую машину, вы можете получить пароль:

```csharp
string password = reader.GetEncryptionPassword();
// Сохраните его в безопасном месте и используйте на новой машине
```

На новой машине:

```csharp
NeoIniReader migrated = new("secure.ini", password);
```

- Если использовался пользовательский пароль (`new NeoIniReader(path, "secret")`), метод `GetEncryptionPassword()` возвращает информационную строку статуса и **не** раскрывает сам пароль.

### Disposal & Lifetime

```csharp
using NeoIniReader reader = new("config.ini");
// работа с reader
// при выходе из блока using:
//  - вызывается SaveFile(), если SaveOnDispose равно true
//  - данные очищаются, а внутренние ресурсы высвобождаются
```

- После вызова Dispose любая попытка использовать экземпляр вызовет исключение `ObjectDisposedException`.

## Advanced features

- Attribute‑based mapping & source generator (1.7+) — [detailed guide](./ATTRIBUTE-MAPPING-RU.md)
- Hot Reload (1.7.1+) — [usage & caveats](./HOT-RELOAD-RU.md)
- Human‑editable INI mode (1.7.2+) — [experimental mode](./HUMAN-MODE-RU.md)

## API Reference

### Core methods

| Метод | Описание | Асинхронная версия |
|--------|-------------|---------------|
| `GetValue<T>` | Читает типизированное значение с дефолтным (опционально с автодобавлением) | `GetValueAsync<T>` |
| `GetValueClamped<T>` | Читает типизированное значение и ограничивает его между min/max | `GetValueClampedAsync<T>` |
| `TryGetValue<T>` | Читает типизированное значение без изменения файла и без AutoAdd | - |
| `SetValue<T>` | Устанавливает/создает ключ-значение | `SetValueAsync<T>` |
| `SetValueClamped<T>` | Устанавливает/создает ключ-значение и ограничивает его в диапазоне | `SetValueClampedAsync<T>` |
| `AddSection` | Создает секцию, если она отсутствует | `AddSectionAsync` |
| `AddKey<T>` | Добавляет уникальный ключ-значение | `AddKeyAsync<T>` |
| `AddKeyClamped<T>` | Добавляет уникальный ключ-значение с ограничением диапазона | `AddKeyClampedAsync<T>` |
| `RemoveKey` | Удаляет конкретный ключ | `RemoveKeyAsync` |
| `RemoveSection` | Удаляет секцию целиком | `RemoveSectionAsync` |
| `ClearSection` | Удаляет все ключи из секции | `ClearSectionAsync` |
| `RenameKey` | Переименовывает ключ в секции | `RenameKeyAsync ` |
| `RenameSection` | Переименовывает секцию целиком | `RenameSectionAsync ` |
| `Search` | Ищет ключи/значения по паттерну | – |
| `FindKey` | Ищет ключ во всех секциях | – |
| `GetAllSections` | Выводит список всех секций | – |
| `GetAllKeys` | Выводит список ключей в секции | – |
| `GetSection` | Получает все пары ключ-значение в секции | – |
| `SectionExists` | Проверяет, существует ли секция | – |
| `KeyExists` | Проверяет, существует ли ключ в секции | – |
| `SaveFile` | Сохраняет данные в файл | `SaveFileAsync` |
| `ToString` | Сериализует INI-данные в форматированную строку (как в файле) | – |
| `ReloadFromFile` | Перезагружает данные из файла | `ReloadFromFileAsync` |
| `DeleteFile` | Удаляет файл с диска | – |
| `DeleteFileWithData` | Удаляет файл и очищает данные из памяти | – |
| `DeleteBackup` | Удаляет файл бэкапа с диска | – |
| `Clear` | Полностью очищает внутреннюю структуру данных | – |
| `GetEncryptionPassword` | Получает пароль шифрования (или статус) | – |
| `CreateAsync` | Асинхронно создает и инициализирует ридер (статическая фабрика) | `CreateAsync (только async)` |

### Options (NeoIniReaderOptions)

| Опция | Описание | По умолчанию |
|--------|-------------|---------|
| `UseAutoSave` | Автоматически сохраняет изменения на диск после модификаций | `true` |
| `AutoSaveInterval` | Количество операций между автосохранениями, если AutoSave включен | `0` (каждое изменение) |
| `UseAutoBackup` | Создает файлы `.backup` во время операций сохранения для безопасности | `true` |
| `UseAutoAdd` | Автоматически создает отсутствующие секции/ключи со значениями по умолчанию при чтении через `GetValue<T>` | `true` |
| `UseChecksum` | Вычисляет и проверяет контрольные суммы во время загрузки/сохранения | `true` |
| `SaveOnDispose` | Автоматически сохраняет конфигурацию при уничтожении экземпляра | `true` |
| `AllowEmptyValues` | Разрешает сохранять ключи конфигурации с пустыми или null значениями | `true` |

### Events

| Событие | Описание |
|--------|-------------|
| `Saved` | Вызывается после сохранения файла на диск |
| `Loaded` | Вызывается после успешной загрузки данных из файла или перезагрузки |
| `KeyChanged` | Вызывается, когда значение существующего ключа в секции изменяется |
| `KeyRenamed` | Вызывается при переименовании ключа в секции |
| `KeyAdded` | Вызывается при добавлении нового ключа в секцию |
| `KeyRemoved` | Вызывается при удалении ключа из секции |
| `SectionChanged` | Вызывается при любом изменении секции (ключи изменены/добавлены/удалены) |
| `SectionRenamed` | Вызывается при переименовании секции |
| `SectionAdded` | Вызывается при добавлении новой секции |
| `SectionRemoved` | Вызывается при удалении секции |
| `DataCleared` | Вызывается при полной очистке данных |
| `AutoSave` | Вызывается перед автоматическим сохранением |
| `ChecksumMismatch` | Вызывается, когда контрольная сумма не совпадает при загрузке файла |
| `SearchCompleted` | Вызывается после каждого поиска с указанием паттерна и количества совпадений |
| `Error` | Вызывается при возникновении ошибок (парсинг, сохранение, чтение файла и т.д.) |

## Philosophy

**Дизайн "Черного ящика" (Black Box Design)**: вся внутренняя логика скрыта за простым публичным API класса `NeoIniReader`. Вы работаете только с методами и событиями, не задумываясь о деталях реализации.
Конфигурационные файлы NeoIni предназначены для управления самой библиотекой, а не для ручного редактирования людьми в "Блокноте" — человеческие комментарии намеренно не сохраняются, и предупреждающий заголовок в файле четко об этом сигнализирует.
