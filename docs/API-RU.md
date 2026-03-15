[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./API.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./API-RU.md)

## API Reference · NeoIni

Полная справка по всем публичным методам, опциям и событиям `NeoIniReader`.

---

### Core Methods

| Method | Description | Async Version |
|--------|-------------|---------------|
| `GetValue<T>` | Чтение типизированного значения с fallback на значение по умолчанию (с опциональным auto-add) | `GetValueAsync<T>` |
| `GetValueClamped<T>` | Чтение типизированного значения с ограничением в диапазоне min/max | `GetValueClampedAsync<T>` |
| `TryGetValue<T>` | Чтение типизированного значения без модификации файла и без AutoAdd | – |
| `SetValue<T>` | Установка/создание ключа со значением | `SetValueAsync<T>` |
| `SetValueClamped<T>` | Установка/создание ключа со значением, ограниченным в диапазоне | `SetValueClampedAsync<T>` |
| `AddSection` | Создание секции, если она отсутствует | `AddSectionAsync` |
| `AddKey<T>` | Добавление уникального ключа со значением | `AddKeyAsync<T>` |
| `AddKeyClamped<T>` | Добавление уникального ключа со значением, ограниченным в диапазоне | `AddKeyClampedAsync<T>` |
| `RemoveKey` | Удаление конкретного ключа | `RemoveKeyAsync` |
| `RemoveSection` | Удаление всей секции | `RemoveSectionAsync` |
| `ClearSection` | Удаление всех ключей из секции | `ClearSectionAsync` |
| `RenameKey` | Переименование ключа в секции | `RenameKeyAsync` |
| `RenameSection` | Переименование всей секции | `RenameSectionAsync` |
| `Search` | Поиск ключей/значений по паттерну | – |
| `FindKey` | Поиск ключа по всем секциям | – |
| `GetAllSections` | Получение списка всех секций | – |
| `GetAllKeys` | Получение списка ключей в секции | – |
| `GetSection` | Получение всех пар ключ-значение в секции | – |
| `SectionExists` | Проверка существования секции | – |
| `KeyExists` | Проверка существования ключа в секции | – |
| `SaveFile` | Сохранение данных в хранилище | `SaveFileAsync` |
| `ToString` | Сериализация INI-данных в форматированную строку (как в файле) | – |
| `ReloadFromFile` | Перезагрузка данных из хранилища | `ReloadFromFileAsync` |
| `DeleteFile` | Удаление файла с диска | – |
| `DeleteFileWithData` | Удаление файла и очистка данных | – |
| `DeleteBackup` | Удаление backup-файла с диска | – |
| `Clear` | Полная очистка внутренней структуры данных | – |
| `GetEncryptionPassword` | Получение пароля шифрования (или статуса) | – |
| `CreateAsync` | Асинхронное создание и инициализация reader (статическая фабрика) | – |
| `CreateHumanMode` | Создание reader в human-editable режиме | `CreateHumanModeAsync` |

---

### Options (NeoIniReaderOptions)

| Option | Description | Default |
|--------|-------------|---------|
| `UseAutoSave` | Автоматическое сохранение изменений в хранилище после модификаций | `true` |
| `AutoSaveInterval` | Количество операций между автоматическими сохранениями при включённом AutoSave | `0` (каждое изменение) |
| `UseAutoBackup` | Создание `.backup`-файлов при сохранении для безопасности | `true` |
| `UseAutoAdd` | Автоматическое создание отсутствующих секций/ключей со значениями по умолчанию при чтении через `GetValue<T>` | `true` |
| `UseChecksum` | Вычисление и проверка контрольных сумм при загрузке/сохранении | `true` |
| `SaveOnDispose` | Автоматическое сохранение конфигурации при вызове Dispose | `true` |
| `AllowEmptyValues` | Разрешение сохранения ключей с пустыми или null значениями | `true` |

**Встроенные пресеты:** `Default`, `Safe`, `Performance`, `ReadOnly`, `BufferedAutoSave(interval)`.

---

### Events

| Event | Description |
|-------|-------------|
| `Saved` | Вызывается после сохранения данных в хранилище |
| `Loaded` | Вызывается после успешной загрузки данных или перезагрузки |
| `KeyChanged` | Вызывается при изменении значения существующего ключа |
| `KeyRenamed` | Вызывается при переименовании ключа в секции |
| `KeyAdded` | Вызывается при добавлении нового ключа в секцию |
| `KeyRemoved` | Вызывается при удалении ключа из секции |
| `SectionChanged` | Вызывается при любом изменении секции (ключи изменены/добавлены/удалены) |
| `SectionRenamed` | Вызывается при переименовании секции |
| `SectionAdded` | Вызывается при добавлении новой секции |
| `SectionRemoved` | Вызывается при удалении секции |
| `DataCleared` | Вызывается при полной очистке данных |
| `AutoSave` | Вызывается перед автоматическим сохранением |
| `ChecksumMismatch` | Вызывается при несовпадении контрольной суммы при загрузке |
| `SearchCompleted` | Вызывается после каждого поиска с паттерном и количеством совпадений |
| `Error` | Вызывается при возникновении ошибок (парсинг, сохранение, чтение и т.д.) |
