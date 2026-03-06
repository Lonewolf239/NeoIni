[![NeoIni](https://img.shields.io/badge/NeoIni-Black%20Box-2D2D2D?style=for-the-badge&logo=lock&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![NuGet](https://img.shields.io/nuget/v/NeoIni?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)
[![.NET 6+](https://img.shields.io/badge/.NET-6+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)

[![MIT](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=heart&logoColor=FFFFFF)](https://opensource.org/licenses/MIT)
[![Thread-Safe](https://img.shields.io/badge/Thread-Safe-2D2D2D?style=for-the-badge&logo=verified&logoColor=FFFFFF)](https://github.com/Lonewolf239/NeoIni)
[![Downloads](https://img.shields.io/nuget/dt/NeoIni?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/NeoIni)

## Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# NeoIni

NeoIni — полнофункциональная библиотека на C# для работы с INI‑файлами, обеспечивающая безопасное, потокобезопасное чтение и запись конфигурации с встроенной проверкой целостности (контрольная сумма) и опциональным AES‑шифрованием.

## Installation

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)  
- **Version:** `1.6` | **.NET 6+**  
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- **Typed Get**: чтение значений как `bool`, `int`, `double`, `DateTime`, `enum`, `string` и других типов с автоматическим разбором и значениями по умолчанию.
- **AutoAdd**: при чтении через `GetValue<T>` отсутствующие ключи/секции могут автоматически создаваться с заданным значением по умолчанию.
- **Thread-safe**: использует `ReaderWriterLockSlim` для безопасного доступа из нескольких потоков.
- **AutoSave**: автоматическое сохранение после изменений или с заданным интервалом (`AutoSave`, `AutoSaveInterval`).
- **AutoBackup**: создание файла `.backup` при сохранении для защиты от порчи данных.
- **Checksum**: встроенная проверка контрольной суммы SHA256 для обнаружения повреждений/подмены.
- **Optional AES-256 encryption**: прозрачное шифрование файла с IV и солью на файл; ключ выводится из окружения пользователя или пользовательского пароля.
- **Full async API**: асинхронные версии всех основных операций (`CreateAsync`, `GetValueAsync`, `SetKeyAsync`, `SaveFileAsync`, `AddSectionAsync` и т.д.).
- **TryGet helpers**: `TryGetValue<T>` / `TryGetValueAsync<T>` для чтения значений **без** модификации файла и без авто‑создания ключей.
- **Convenient API**: удобные методы для управления секциями и ключами (создание, переименование, поиск, очистка, удаление).
- **Events**: события для сохранения, загрузки, изменения ключей/секций, автосохранения, ошибок, несовпадения контрольной суммы и завершения поиска.
- **Easy migration**: перенос зашифрованных конфигов между машинами через `GetEncryptionPassword()` при использовании авто‑шифрования.

## Security Features

- **Checksum (SHA256)**: при сохранении к содержимому файла добавляется 32‑байтовая контрольная сумма, вычисленная через SHA256; при чтении она проверяется, и при несовпадении можно обработать событие `OnChecksumMismatch` или откатиться к `.backup`.
- **AES-256**: при включённом шифровании данные шифруются AES в режиме CBC с 16‑байтовым IV и 32‑байтовым ключом, полученным из пароля (заданного окружением или пользователем) и случайной 16‑байтовой соли, хранящейся в файле.
- **Environment-based key**: в режиме авто‑шифрования ключ детерминированно выводится из `Environment.UserName`, `Environment.MachineName` и `Environment.UserDomainName` плюс соль файла, что делает файл нечитаемым на другой машине без специального пароля.
- **Backup fallback**: при ошибках чтения, несоответствии контрольной суммы или ошибке расшифровки библиотека сначала пытается прочитать `.backup`‑файл.
- **Thread-safe access**: все операции чтения/записи обёрнуты в `ReaderWriterLockSlim`, что предотвращает гонки при высокой нагрузке.

## Quick Start

### Creating a NeoIniReader Instance

#### Synchronous

```csharp
using NeoIni;

// Без шифрования
NeoIniReader reader = new("config.ini");

// Авто‑шифрование, привязка к окружению (машина/пользователь)
NeoIniReader encryptedReader = new("config.ini", autoEncryption: true);

// Шифрование с пользовательским паролем (файл можно перенести на другую машину)
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

- При `autoEncryption = true` ключ генерируется автоматически и привязывается к окружению пользователя/машины.  
- Если передан `encryptionPassword`, ключ выводится из этого пароля и соли файла — удобно для переноса конфигураций между машинами.

### Reading Values

```csharp
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
DateTime when = reader.GetValue<DateTime>("Log", "LastRun", DateTime.Now);
```

Async:

```csharp
string text = await reader.GetValueAsync("Section1", "Key1", "default");
int number = await reader.GetValueAsync("Section1", "Number", 0);
bool flag = await reader.GetValueAsync("Section1", "Enabled", false);
double value = await reader.GetValueAsync("Section1", "Value", 0.0);
DateTime when = await reader.GetValueAsync("Log", "LastRun", DateTime.Now, cancellationToken);
```

- Если секция/ключ отсутствуют, возвращается `defaultValue`; при включённом `AutoAdd` ключ может автоматически добавляться в файл с этим значением.  
- Чтение `enum` и `DateTime` поддерживается через `Enum.TryParse`, `DateTime.TryParse` и `Convert.ChangeType` (инвариантная культура).

### TryGet (without AutoAdd or file modification)

Если нужно просто прочитать значение **без** авто‑создания ключей и любых изменений файла, используйте `TryGetValue` / `TryGetValueAsync`:

```csharp
// Синхронно
int level = reader.TryGetValue("Game", "Level", defaultValue: 1);

// Асинхронно
int levelAsync = reader.TryGetValueAsync("Game", "Level", defaultValue: 1, cancellationToken);
```

- Эти методы **никогда** не пишут в файл и не зависят от `AutoAdd`: если секция или ключ отсутствуют, просто возвращается `defaultValue`.

### Writing Values

```csharp
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
reader.SetKey("Section1", "LastUpdate", DateTime.Now);
```

Async:

```csharp
await reader.SetKeyAsync("Section1", "Key1", "Value1");
await reader.SetKeyAsync("Section1", "Number", 42, cancellationToken);
```

- Если секция/ключ отсутствуют, они будут созданы; изменения вызывают `OnKeyAdded` / `OnKeyChanged` и при необходимости могут запускать автосохранение.

### Example

```csharp
using NeoIni;

using NeoIniReader reader = new("config.ini");

// Инициализация настроек БД
reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

// Чтение
string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"DB: {host}:{port}");
```

- При первом запуске файл будет создан; далее значения просто читаются и переиспользуются/обновляются.

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

- Методы управления секциями/ключами доступны как в синхронных, так и в асинхронных вариантах, где это имеет смысл.

### File Operations

```csharp
// Явное сохранение
reader.SaveFile();
await reader.SaveFileAsync();

// Перечитать данные с диска
reader.ReloadFromFile();

// Удаление файла
reader.DeleteFile();         // только файл
reader.DeleteFileWithData(); // файл + очистка Data
```

- При сохранении используется временный `.tmp`‑файл; если `AutoBackup` включён, создаётся `.backup`, который затем используется как резервная копия при ошибках чтения.

### Options

```csharp
reader.AutoSave = true;        // включить автосохранение
reader.AutoSaveInterval = 3;   // сохранять каждые 3 операции записи (если AutoSave включён)

reader.AutoBackup = true;      // включить .backup
reader.AutoAdd = true;         // авто‑создание ключей в GetValue
reader.UseChecksum = true;     // включить контрольные суммы
reader.SaveOnDispose = true;   // сохранять при Dispose()
```

Также можно использовать пресеты `NeoIniReaderOptions` при создании экземпляра (`Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(interval)`).

### Events (Callbacks)

```csharp
reader.OnSave += () => Console.WriteLine("Saved");
reader.OnLoad += () => Console.WriteLine("Loaded");

reader.OnKeyChanged += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} changed to {value}");

reader.OnKeyAdded += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} added: {value}");

reader.OnKeyRemoved += (section, key) =>
    Console.WriteLine($"[{section}] {key} removed");

reader.OnSectionAdded += section =>
    Console.WriteLine($"Section added: {section}");

reader.OnSectionRemoved += section =>
    Console.WriteLine($"Section removed: {section}");

reader.OnSectionChanged += section =>
    Console.WriteLine($"Section changed: {section}");

reader.OnChecksumMismatch += (expected, actual) =>
    Console.WriteLine("Checksum mismatch detected!");

reader.OnAutoSave += () =>
    Console.WriteLine("AutoSave triggered");

reader.OnError += ex =>
    Console.WriteLine($"Error: {ex.Message}");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var (section, key, value) in results)
    Console.WriteLine($"[{section}] {key} = {value}");
```

- Поиск выполняется по ключам и значениям (без учёта регистра); результат — список кортежей `(section, key, value)`. После поиска вызывается `OnSearchCompleted` с шаблоном и числом совпадений.

### Encryption & Migration

#### Auto-encryption (machine-bound)

```csharp
NeoIniReader reader = new("secure.ini", autoEncryption: true);
```

- Ключ детерминированно выводится из текущего пользователя/машины/домена плюс случайной соли файла. Такой файл нельзя прочитать на другой машине без использования сгенерированного пароля.

Для переноса на другую машину можно получить пароль:

```csharp
string password = reader.GetEncryptionPassword();
// Сохраните его где‑нибудь безопасно и используйте на новой машине
```

На новой машине:

```csharp
NeoIniReader migrated = new("secure.ini", password);
```

- Если использовался пользовательский пароль (`new NeoIniReader(path, "secret")`), `GetEncryptionPassword()` вернёт информационную строку‑статус и **не** раскроет сам пароль.

### Disposal & Lifetime

```csharp
using NeoIniReader reader = new("config.ini");
// работа с reader
// при выходе из using‑блока:
//  - вызывается SaveFile(), если SaveOnDispose == true
//  - Data очищается, внутренние ресурсы освобождаются
```

- После вызова `Dispose()` любые обращения к экземпляру приводят к `ObjectDisposedException`.

## API Reference

### Core methods

| Method | Description | Async Version |
|--------|-------------|---------------|
| `GetValue<T>` | Чтение типизированного значения с значением по умолчанию (при желании с авто‑созданием) | `GetValueAsync<T>` |
| `GetValueClamp<T>` | Чтение значения и ограничение его диапазоном min/max | `GetValueClampAsync<T>` |
| `TryGetValue<T>` | Чтение значения без модификации файла и без AutoAdd | `TryGetValueAsync<T>` |
| `SetKey<T>` | Установка/создание пары ключ‑значение | `SetKeyAsync<T>` |
| `AddSection` | Создание секции, если её нет | `AddSectionAsync` |
| `AddKeyInSection<T>` | Добавление нового ключа‑значения | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Удаление конкретного ключа | `RemoveKeyAsync` |
| `RemoveSection` | Удаление целой секции | `RemoveSectionAsync` |
| `ClearSection` | Удаление всех ключей из секции | `ClearSectionAsync` |
| `RenameKey` | Переименование ключа в секции | `RenameKeyAsync` |
| `RenameSection` | Переименование целой секции | `RenameSectionAsync` |
| `Search` | Поиск по шаблону среди ключей/значений | – |
| `FindKeyInAllSections` | Поиск ключа во всех секциях | – |
| `GetAllSections` | Получение списка всех секций | – |
| `GetAllKeys` | Получение списка ключей секции | – |
| `GetSection` | Получение всех пар ключ‑значение секции | – |
| `SectionExists` | Проверка существования секции | – |
| `KeyExists` | Проверка существования ключа в секции | – |
| `SaveFile` | Сохранение данных в файл | `SaveFileAsync` |
| `ToString` | Сериализация данных INI в строку в формате файла | – |
| `ReloadFromFile` | Перезагрузка данных из файла | – |
| `DeleteFile` | Удаление файла с диска | – |
| `DeleteFileWithData` | Удаление файла и очистка данных | – |
| `GetEncryptionPassword` | Получение пароля шифрования (или статуса) | – |

### Options (NeoIniReaderOptions)

| Option | Description | Default |
|--------|-------------|---------|
| `AutoSave` | Автоматически сохранять изменения на диск после модификаций | `true` |
| `AutoSaveInterval` | Количество операций между автосохранениями при включённом AutoSave | `0` (каждое изменение) |
| `AutoBackup` | Создавать `.backup` при сохранении для безопасности | `true` |
| `AutoAdd` | Автоматически создавать отсутствующие секции/ключи при `GetValue<T>` | `true` |
| `UseChecksum` | Вычислять и проверять контрольную сумму при чтении/записи | `true` |
| `SaveOnDispose` | Автоматически сохранять конфигурацию при Dispose | `true` |

### Events

| Action | Description |
|--------|-------------|
| `OnSave` | Вызывается после сохранения файла на диск |
| `OnLoad` | Вызывается после успешной загрузки данных из файла или повторной загрузки |
| `OnKeyChanged` | Вызывается при изменении значения существующего ключа |
| `OnKeyAdded` | Вызывается при добавлении нового ключа в секцию |
| `OnKeyRemoved` | Вызывается при удалении ключа из секции |
| `OnSectionChanged` | Вызывается при изменении секции (изменение/добавление/удаление ключей) |
| `OnSectionAdded` | Вызывается при добавлении новой секции |
| `OnSectionRemoved` | Вызывается при удалении секции |
| `OnDataCleared` | Вызывается при полном очищении данных |
| `OnAutoSave` | Вызывается перед автосохранением |
| `OnChecksumMismatch` | Вызывается при несовпадении контрольной суммы при загрузке файла |
| `OnSearchCompleted` | Вызывается после каждого поиска с шаблоном и числом совпадений |
| `OnError` | Вызывается при ошибках (парсинг, сохранение, чтение файла и т.п.) |

## Philosophy

**Black Box Design**: вся внутренняя логика скрыта за простым публичным API класса `NeoIniReader`. Вы работаете только с методами и событиями, не задумываясь о деталях реализации.  
Конфигурационные файлы NeoIni принадлежат и управляются самой библиотекой, а не людьми, правящими их в блокноте — пользовательские комментарии намеренно не сохраняются, а предупреждающий заголовок прямо об этом говорит.
