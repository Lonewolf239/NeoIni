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

NeoIni — это полнофункциональная C#‑библиотека для работы с INI‑файлами, которая обеспечивает безопасное, потокобезопасное чтение/запись конфигурации с встроенной проверкой целостности (checksum) и опциональным AES‑шифрованием.

## Installation

```bash
dotnet add package NeoIni
```

- **Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)
- **Version:** `1.5.8.1` | **.NET 6+**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- Typed Get: чтение значений как `bool`, `int`, `double`, `DateTime`, `enum`, `string` и др. с автоматическим парсингом и дефолтами.
- AutoAdd: при чтении через `GetValue<T>` отсутствующие ключи/секции могут автоматически создаваться с дефолтным значением.
- Thread‑safe: использование `ReaderWriterLockSlim` для безопасного доступа из нескольких потоков.
- AutoSave: автоматическое сохранение после изменений или через интервал операций (`UseAutoSaveInterval`, `AutoSaveInterval`).
- AutoBackup: создание `.backup`‑файла при сохранении для защиты от повреждения.
- Checksum: встроенная проверка SHA256‑контрольной суммы для обнаружения повреждений/подмены данных.
- Optional AES‑256 encryption: прозрачное шифрование на уровне файла с IV и ключом, привязанным к окружению пользователя или к кастомному паролю.
- Полный async‑API: асинхронные аналоги для большинства операций (`GetValueAsync`, `SetKeyAsync`, `SaveFileAsync`, `ReloadFromFileAsync`, и т.д.).
- Удобный API для управления секциями и ключами (создание, переименование, поиск, очистка, удаление).
- События: хуки на сохранение, загрузку, изменения ключей/секций, автосохранение, ошибки, несоответствие checksum и завершение поиска.
- Простое мигрирование зашифрованных конфигов между машинами через `GetEncryptionPassword()` при авто‑шифровании.

## Security Features

- **Checksum (SHA256)**: при сохранении к содержимому файла добавляется 8‑байтная контрольная сумма, вычисленная через SHA256; при чтении она проверяется, а при несоответствии можно обработать событие `OnChecksumMismatch` или откатиться к `.backup`.
- **AES‑256**: при включённом шифровании данные шифруются AES с 16‑байтовым IV, ключ длиной 32 байта генерируется из окружения пользователя или на основе пользовательского пароля.
- **Environment‑based key**: по умолчанию ключ детерминированно генерируется из `Environment.UserName`, `Environment.MachineName` и `Environment.UserDomainName`, что делает файл нечитаемым на другом хосте без специального пароля.
- **Backup fallback**: при ошибке чтения, повреждении или ошибке расшифровки библиотека может автоматически попробовать прочитать `.backup`‑файл.
- **Thread‑safe access**: все операции чтения/записи обернуты в `ReaderWriterLockSlim`, что предотвращает гонки при высоконагруженной работе.

## Quick Start

### Creating a NeoIniReader Instance

```csharp
using NeoIni;

NeoIniReader reader = new("config.ini");                             // Без шифрования
NeoIniReader encryptedReader = new("config.ini", true);              // Авто-шифрование по окружению
NeoIniReader customEncrypted = new("config.ini", "MySecretPas123");  // Шифрование по своему паролю
```

- При `autoEncryption = true` ключ генерируется автоматически и привязывается к пользователю/машине.
- При переданном `encryptionPassword` ключ детерминированно строится из указанного пароля, что удобно для переносимых конфигов между машинами.

### Reading Values

```csharp
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
DateTime when = reader.GetValue<DateTime>("Log", "LastRun", DateTime.Now);
```

- При отсутствии секции/ключа возвращается `defaultValue`, при включённом `AutoAdd` ключ при этом может быть создан в файле.
- Поддерживается чтение `enum` и `DateTime` через `Convert.ChangeType`/`Enum.TryParse`/`DateTime.TryParse` (инвариантная культура).

### Writing Values

```csharp
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
reader.SetKey("Section1", "LastUpdate", DateTime.Now);
```

- Если секции/ключа нет, они будут созданы; изменения при необходимости вызывают события `OnKeyAdded`/`OnKeyChanged` и могут триггерить автосохранение.

## Example

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

- В этом примере при первом запуске файл будет создан, при последующих — значения просто читаются и обновляются.

### Section/Key Management

```csharp
// Проверки
bool sectionExists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");

// Создание/удаление
reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
reader.RemoveSection("Section1");

// Получение списка
string[] sections = reader.GetAllSections();
string[] keys = reader.GetAllKeys("NewSection");

// Очистка
reader.ClearSection("NewSection");
```

- Методы секционно‑ключевого менеджмента доступны как в sync, так и в async вариантах (`AddSectionAsync`, `RemoveKeyAsync`, `RenameSectionAsync`, и т.д.).

### File Operations

```csharp
// Явное сохранение
reader.SaveFile();
await reader.SaveFileAsync();

// Перечитать данные с диска
reader.ReloadFromFile();
await reader.ReloadFromFileAsync();

// Удаление файла
reader.DeleteFile();                // только файл
reader.DeleteFileWithData();        // файл + очистка Data
```

- При сохранении используется промежуточный `.tmp` и, при включённом `AutoBackup`, создаётся `.backup`, после чего происходит атомарная замена.

### Options

```csharp
reader.AutoSave = true;             // включить автосохранение
reader.AutoSaveInterval = 3;        // сохранять не каждое изменение, а каждые 3 операций

reader.AutoBackup = true;           // включить .backup
reader.AutoAdd = true;              // автосоздание ключей при GetValue
reader.UseChecksum = true;          // включить checksum
```

### Events (Callbacks)

```csharp
reader.OnSave += () => Console.WriteLine("Saved");
reader.OnLoad += () => Console.WriteLine("Loaded");

reader.OnKeyChanged += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} changed to {value}");

reader.OnKeyAdded += (section, key, value) =>
    Console.WriteLine($"[{section}] {key} added: {value}");

reader.OnSectionAdded += section =>
    Console.WriteLine($"Section added: {section}");

reader.OnChecksumMismatch += (expected, actual) =>
    Console.WriteLine("Checksum mismatch detected!");

reader.OnError += ex =>
    Console.WriteLine($"Error: {ex.Message}");
```

### Search

```csharp
var results = reader.Search("token");
foreach (var (section, key, value) in results)
    Console.WriteLine($"[{section}] {key} = {value}");
```

- Поиск ведётся по ключам и значениям (case‑insensitive), результат — список кортежей `(section, key, value)`. После поиска вызывается `OnSearchCompleted` с шаблоном и числом совпадений.

### Encryption & Migration

#### Auto-encryption (machine-bound)

```csharp
NeoIniReader reader = new("secure.ini", autoEncryption: true);
```

- Ключ генерируется детерминированно из текущего пользователя/машины/домена, поэтому файл нельзя прочитать на другой машине без знания сгенерированного пароля.

Для миграции на другую машину можно получить пароль:

```csharp
string password = reader.GetEncryptionPassword();
// Сохранить где-то безопасно и использовать на новой машине
```

На новой машине:

```csharp
NeoIniReader migrated = new("secure.ini", password);
```

- Если использовался кастомный пароль (`NeoIniReader(path, "secret")`), `GetEncryptionPassword()` вернёт статус, не раскрывая пароль.

### Disposal & Lifetime

```csharp
using NeoIniReader reader = new("config.ini");
// работа с reader
// при выходе из using:
//  - вызывается SaveFile()
//  - очищаются данные и освобождаются ресурсы
```

- В `Dispose(bool)` вызывается `SaveFile` (с подавлением исключений), очищается внутренний `Data` и освобождается `ReaderWriterLockSlim`, после чего объект считается освобождённым.

## API Reference

| Метод | Описание | Асинхронная версия |
|-------|----------|--------------------|
| `GetValue<T>` | Чтение типизированного значения с возвратом по умолчанию | `GetValueAsync<T>` |
| `GetValueClamp<T>` | Читает типизированное значение и ограничивает его заданным диапазоном (min/max) | `GetValueClampAsync<T>` |
| `SetKey<T>` | Установка/создание пары ключ-значение | `SetKeyAsync<T>` |
| `AddSection` | Создание секции при отсутствии | `AddSectionAsync` |
| `AddKeyInSection<T>` | Добавление уникальной пары ключ-значение | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Удаление конкретного ключа | `RemoveKeyAsync` |
| `RemoveSection` | Удаление целой секции | `RemoveSectionAsync` |
| `GetAllSections` | Список всех секций | - |
| `GetAllKeys` | Список ключей секции | - |
| `GetSection` | Получение всех пар ключ-значение секции | - |
| `FindKeyInAllSections` | Поиск ключа по всем секциям | - |
| `ClearSection` | Удаление всех ключей из секции | `ClearSectionAsync` |
| `RenameKey` | Переименование ключа в секции | `RenameKeyAsync` |
| `RenameSection` | Переименование секции | `RenameSectionAsync` |
| `Search` | Поиск ключей/значений по шаблону | - |
| `SectionExists` | Проверка существования секции | - |
| `KeyExists` | Проверка существования ключа в секции | - |
| `SaveFile` | Сохранение данных в файл | `SaveFileAsync` |
| `ToString` | Сериализация данных INI в отформатированную строку (как в файле) | - |
| `ReloadFromFile` | Перезагрузка данных из файла | - |
| `DeleteFile` | Удаление файла с диска | - |
| `DeleteFileWithData` | Удаление файла и очистка данных | - |
| `GetEncryptionPassword` | Получение пароля шифрования (или статуса) | - |

| Опция | Описание | Значение по умолчанию |
|-------|----------|-----------------------|
| `AutoSave` | Автоматическое сохранение изменений на диск после модификаций | `true` |
| `AutoSaveInterval` | Количество операций между автоматическими сохранениями при включенной функции автосохранения | `0` |
| `AutoBackup` | Создание резервных копий (.backup) при операциях сохранения | `true` |
| `AutoAdd` | Автоматическое создание отсутствующих секций/ключей со значениями по умолчанию при чтении через `GetValue<T>` | `true` |
| `UseChecksum` | Вычисление и проверка контрольных сумм при загрузке/сохранении для обнаружения повреждений | `true` |
| `SaveOnDispose` | Автоматически сохраняет конфигурацию при освобождении (удалении) экземпляра | `true` |

| Событие | Описание |
|---------|----------|
| `OnSave` | Вызывается после сохранения файла на диск |
| `OnLoad` | Вызывается после успешной загрузки данных из файла или перезагрузки |
| `OnKeyChanged` | Вызывается при изменении значения существующего ключа в секции |
| `OnKeyAdded` | Вызывается при добавлении нового ключа в секцию |
| `OnKeyRemoved` | Вызывается при удалении ключа из секции |
| `OnSectionChanged` | Вызывается при любом изменении секции (изменение/добавление/удаление ключей) |
| `OnSectionAdded` | Вызывается при добавлении новой секции |
| `OnSectionRemoved` | Вызывается при удалении секции |
| `OnDataCleared` | Вызывается при полной очистке данных |
| `OnAutoSave` | Вызывается перед автоматическим сохранением |
| `OnError` | Вызывается при ошибках (парсинг, сохранение, чтение файла и т.д.) |

## Philosophy

**Black Box Design**: вся внутренняя логика скрыта за простым публичным API класса `NeoIniReader`. Вы работаете только с методами и событиями, не думая о деталях реализации.
