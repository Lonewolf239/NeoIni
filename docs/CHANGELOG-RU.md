[![EN](https://img.shields.io/badge/CHANGELOG-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CHANGELOG.md)
[![RU](https://img.shields.io/badge/CHANGELOG-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CHANGELOG-RU.md)

## Changelog · NeoIni

<details open>
<summary><strong>3.1</strong> — 21 марта 2026</summary>

#### List of changes

- **Критическое изменение**: интерфейс `IEncryptionProvider` дополнен методами шифрования/дешифрования:
  - `void Encrypt(MemoryStream, byte[] key, byte[] salt, byte[] plaintextBytes)`
  - `Task EncryptAsync(MemoryStream, byte[] key, byte[] salt, byte[] plaintextBytes, CancellationToken ct)`
  - `byte[] Decrypt(byte[] key, byte[] iv, byte[] encryptedBytes)`
  - `Task<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] encryptedBytes, CancellationToken ct)`
- Логика шифрования перенесена из `NeoIniFileProvider` в `NeoIniEncryptionProvider` (встроенная реализация AES)
- `NeoIniFileProvider` теперь делегирует фактическое шифрование экземпляру `IEncryptionProvider`

</details>

<details>
<summary><strong>3.0</strong> — 21 марта 2026</summary>

#### List of changes

- Переработаны конструкторы класса `NeoIniDocument`
- Добавлено перечисление `EncryptionType`

</details>

<details>
<summary><strong>2.0</strong> — 21 марта 2026</summary>

#### List of changes

- Улучшен метод `NeoIniFileProvider.RaiseError`
- Улучшена обработка ошибок в методе `NeoIniDocument.StartHotReload`
- Удалён дублирующийся код в `NeoIniFileProvider`

</details>

<details>
<summary><strong>2.0-pre1</strong> — 20 марта 2026</summary>

#### List of changes

- **Крупное переименование**: `NeoIniReader` переименован в `NeoIniDocument`, чтобы лучше отражать его предназначение как документа конфигурации
- **Новая абстракция шифрования**: Добавлен интерфейс `IEncryptionProvider`, позволяющий использовать пользовательские алгоритмы шифрования (AES остаётся реализацией по умолчанию)
- **Подключаемая горячая перезагрузка**: Добавлен интерфейс `IHotReloadMonitor` с монитором на основе файлов по умолчанию; позволяет реализовать собственные стратегии отслеживания изменений
- **Переименован класс параметров**: `NeoIniReaderOptions` → `NeoIniOptions` для согласованности
- **Согласованность API**: `ReloadFromFile`/`ReloadFromFileAsync` переименованы в `Reload`/`ReloadAsync`
- **Структура кода**: Разделён `NeoIniFileProvider` на несколько файлов с частичным классом для улучшения сопровождаемости

</details>

<details>
<summary><strong>1.9</strong> — 19 марта 2026</summary>

#### List of changes

- Небольшой рефакторинг кода
- Исправлены мелкие ошибки

</details>

<details>
<summary><strong>1.9-pre2</strong> — 18 марта 2026</summary>

#### List of changes

- Улучшена реализация `AsyncReaderWriterLock` для повышения производительности и надёжности
- Заменены обширные блоки `catch (Exception)` на конкретную обработку исключений в `NeoIniFileProvider` и `NeoIniParser`
- Проведён небольшой рефакторинг кода для улучшения читаемости и сопровождаемости
- Включены `nullable reference types` и устранены все связанные предупреждения и ошибки по всей кодовой базе

</details>

<details>
<summary><strong>1.9-pre1</strong> — 17 марта 2026</summary>

#### List of changes

- `System.Threading.ReaderWriterLock` заменён на `NeoIni.Models.AsyncReaderWriterLock`

</details>

<details>
<summary><strong>1.8</strong> — 16 марта 2026</summary>

#### List of changes

- Добавлены пропущенные вызовы `ConfigureAwait(false)`
- Добавлен параметр `CancellationToken` в метод `FinalizeSave`
- Улучшена обработка некорректных входных данных в методах `Set`

</details>

<details>
<summary><strong>1.8-pre1</strong> — 15 марта 2026</summary>

#### List of changes

- Исправлено отображение экранирования в методе `Search`
- Устранены излишние выделения памяти
- Добавлена опциональная поддержка значений в кавычках через параметр `UseShielding` (например, `key = "value ; not a comment"`)
- Проведён рефакторинг кода для устранения дублирования

</details>

<details>
<summary><strong>1.7.3</strong> — 15 марта 2026</summary>

#### List of changes

- Добавлен интерфейс `INeoIniProvider` для подключаемых бэкендов хранения (база данных, удалённое хранилище, память и т.д.)
- Реализован `NeoIniFileProvider`, который инкапсулирует всю существующую логику работы с файлами
- Добавлены конструкторы `NeoIniReader`, принимающие `INeoIniProvider` (синхронный, асинхронный, человекочитаемый режим)
- Введено исключение `UnsupportedProviderOperationException` для операций, специфичных для файлов, при использовании пользовательских провайдеров
- В `ApplyOptions` добавлена защита `UseAutoBackup`, предотвращающая сбои для провайдеров, не работающих с файлами
- Класс `NeoIniData` сделан публичным, чтобы позволить реализовывать пользовательские провайдеры

</details>

<details>
<summary><strong>1.7.2</strong> — 13 марта 2026</summary>

#### List of changes

- Исправлена уязвимость при передаче значений в методы `Set`

</details>

<details>
<summary><strong>1.7.2-pre1</strong> — 13 марта 2026</summary>

#### List of changes

- Добавлен режим INI, удобный для ручного редактирования (`HumanMode`) с автоматическим сохранением комментариев и их повторной выдачей
- Введён опциональный «человекочитаемый» конвейер, который сохраняет позиции комментариев относительно секций и ключей
- Автоматическое отключение проверки контрольной суммы при включении `HumanMode` для возможности ручного редактирования
- Запрещено одновременное использование `HumanMode` с шифрованием во избежание повреждения данных и проблем с пользовательским опытом

</details>

<details>
<summary><strong>1.7.1</strong> — 13 марта 2026</summary>

#### List of changes

- Добавлена функциональность горячей перезагрузки с помощью отслеживания файлов

</details>

<details>
<summary><strong>1.7</strong> — 12 марта 2026</summary>

#### List of changes

- Удалён устаревший и неиспользуемый код из `NeoIniMappingGenerator` для упрощения реализации генератора исходного кода

</details>

<details>
<summary><strong>1.7-pre1</strong> — 12 марта 2026</summary>

#### List of changes

- Добавлен мини-пакет `NeoIni.Annotations` с атрибутом `NeoIniKeyAttribute` для привязки свойств модели к конкретным секциям и ключам INI, включая опциональное значение по умолчанию
- Добавлен мини-пакет `NeoIni.Generators` с `NeoIniMappingGenerator` (`IIncrementalGenerator`), который сканирует свойства, помеченные `NeoIniKeyAttribute`, и генерирует `NeoIniReaderExtensions`, содержащий строго типизированные API для получения/установки объектов
- Сгенерированный метод `NeoIniReaderExtensions.Get` создаёт и заполняет экземпляры конфигурации из INI-файлов, используя `GetValue` для каждого сопоставленного свойства, применяя значения по умолчанию из атрибутов или типов, если значения отсутствуют
- Сгенерированный метод `NeoIniReaderExtensions.Set` записывает экземпляры конфигурации обратно в INI-файлы через `SetValue`, выбрасывая `NotSupportedException` для типов, не охваченных маппингом генератора

</details>

<details>
<summary><strong>1.6.1</strong> — 12 марта 2026</summary>

#### List of changes

- Исправлено экранирование символов обратной косой черты (`\`) в парсере для предотвращения повреждения путей Windows
- Исправлена проблема, из-за которой сохранение полностью пустой конфигурации (все секции удалены) игнорировалось
- Исправлена логика парсинга в `TryMatchKey` для корректной загрузки ключей с пустыми значениями, когда включена опция `AllowEmptyValues`
- Добавлена проверка на null (`?? string.Empty`) в `SaveFile` и `SaveFileAsync` для предотвращения `ArgumentNullException`
- Улучшено управление памятью `CryptoStream` с помощью параметра `leaveOpen: true` для предотвращения преждевременного освобождения потока

</details>

<details>
<summary><strong>1.6.1-pre2</strong> — 12 марта 2026</summary>

#### List of changes

- Исправлен расчёт минимальной длины файла в `NeoIniFileProvider` с учётом `SaltSize`
- Реализован автоматический возврат к получению пароля при открытии автоматически зашифрованного файла с пользовательским паролем

</details>

<details>
<summary><strong>1.6.1-pre1</strong> — 12 марта 2026</summary>

#### List of changes

- Делегаты `Action` заменены на `EventHandler`
- Добавлено поле `AllowEmptyValues` для управления пустыми состояниями данных
- Улучшена общая оптимизация кода и производительность выполнения
- Добавлены новые методы API с ограничением значений (`clamped`):
  - `AddKeyClamped<T>` (`AddKeyClampedAsync<T>`)
  - `SetValueClamped<T>` (`SetValueClampedAsync<T>`)
- Переименованы методы API для согласованности: `AddKeyInSection` -> `AddKey` (включая асинхронные варианты) и `SetKey` -> `SetValue` (включая асинхронные варианты)

</details>

<details>
<summary><strong>1.6.0.1</strong> — 6 марта 2026</summary>

#### List of changes

- Исправлено присваивание флага `hasSalt`

</details>

<details>
<summary><strong>1.6</strong> — 6 марта 2026</summary>

#### List of changes

- Переработана система шифрования с использованием PBKDF2 с уникальной солью на файл вместо соли, производной от пароля
- Встроена случайная 16-байтовая соль вместе с вектором инициализации (IV) в формат файла для зашифрованных конфигураций
- Обновлена генерация ключа как для режима автоматического шифрования (на основе окружения), так и для режима пользовательского пароля
- Добавлен вспомогательный класс `NeoIniIO` для централизации буферизированных операций чтения/записи файлов
- Переработан асинхронный ввод-вывод с использованием истинно асинхронных операций `FileStream` с поддержкой отмены
- Добавлен асинхронный путь загрузки данных в `NeoIniFileProvider` и `NeoIniReader` (загрузка конфигурации из базы данных)
- Реализованы асинхронные фабричные методы `CreateAsync` (обычный, с автоматическим шифрованием, с паролем) для неблокирующей инициализации
- Исправлены и унифицированы асинхронные варианты основных публичных методов API (`GetValueAsync`, `SetKeyAsync`, операции с секциями/ключами)
- Улучшена потокобезопасность и обработка отмены в асинхронных методах (использование блокировок + проверки `CancellationToken`)

</details>

<details>
<summary><strong>1.5.x и ниже</strong> — ранняя активная разработка. Библиотека находилась в процессе формирования: API часто менялись, ошибки были обычным делом, и многие выпуски представляли собой инкрементные исправления, а не запланированные функции. Стабильное использование начинается с <strong>1.6.1</strong></summary>

<details>
<summary><strong>1.5.8.2</strong> — 5 марта 2026</summary>

#### List of changes

- Добавлен `NeoIniReaderOptions` для настройки поведения `NeoIniReader`, включая предопределённые пресеты

</details>

<details>
<summary><strong>1.5.8.1</strong> — 5 марта 2026</summary>

#### List of changes

- Улучшена логика экранирования символов новой строки

</details>

<details>
<summary><strong>1.5.8</strong> — 5 марта 2026</summary>

#### List of changes

- Переработан `NeoIniFileProvider`:
  - Добавлен заголовок информации о файле
  - Реализовано автоматическое дешифрование зашифрованных файлов при чтении, если экземпляр создан с отключённым шифрованием, а файл зашифрован в автоматическом режиме
  - Улучшена логика чтения файла: теперь данные файла всегда читаются корректно, независимо от того, как файл был открыт (ранее открытие файла с контрольной суммой, но без её указания, могло вызывать ошибку)
- Добавлен метод `NeoIniReader.ToString()`
- Удалён избыточный и неиспользуемый код

</details>

<details>
<summary><strong>1.5.7.9</strong> — 5 марта 2026</summary>

#### List of changes

- Добавлено отключение предупреждения при отключении контрольной суммы
- Исправлено асинхронное сохранение
- Исправлено экранирование переносов строк в значениях

</details>

<details>
<summary><strong>1.5.7.8</strong> — 10 февраля 2026</summary>

#### List of changes

- Добавлена поддержка многострочных значений

</details>

<details>
<summary><strong>1.5.7.7</strong> — 5 февраля 2026</summary>

#### List of changes

- Исправлено чтение файла

</details>

<details>
<summary><strong>1.5.7.6</strong> — 5 февраля 2026</summary>

#### List of changes

- Метод `Dispose` наконец-то исправлен

</details>

<details>
<summary><strong>1.5.7.5</strong> — 5 февраля 2026</summary>

#### List of changes

- Метод `Dispose` исправлен

</details>

<details>
<summary><strong>1.5.7.4</strong> — 5 февраля 2026</summary>

#### List of changes

- Улучшена логика чтения/записи файлов
- Код из `NeoIniReader` перемещён в `NeoIniReaderCore`

</details>

<details>
<summary><strong>1.5.7.3</strong> — 4 февраля 2026</summary>

#### List of changes

- Логика вынесена в отдельные методы
- Исправлена ошибка чтения при отключении `UseChecksum`

</details>

<details>
<summary><strong>1.5.7.2</strong> — 4 февраля 2026</summary>

#### List of changes

- Добавлен `NeoIniReader.GetValueClamp<T>`
- Изменено предупреждающее сообщение в файле

</details>

<details>
<summary><strong>1.5.7.1</strong> — 4 февраля 2026</summary>

#### List of changes

- Переработана потокобезопасность
- Исправлена критическая ошибка потокобезопасности

</details>

<details>
<summary><strong>1.5.7</strong> — 4 февраля 2026</summary>

#### List of changes

- Заменён объект `Lock` на `ReaderWriterLockSlim` для улучшенной потокобезопасности и параллельного чтения
- Реализован корректный шаблон `IDisposable` с методом `Dispose(bool disposing)` и отслеживанием состояния освобождения
- `OnError` и `OnChecksumMismatch` изменены с полей на свойства, делегирующие вызовы `FileProvider`
- Удалено свойство `UseAutoSaveInterval`; упрощена логика интервала автосохранения
- Значение по умолчанию `AutoSaveInterval` изменено с 3 на 0
- Управление блокировками перенесено в отдельные методы вместо передачи в парсер
- Добавлена проверка `ThrowIfDisposed()`, предотвращающая операции над освобождёнными объектами

</details>

<details>
<summary><strong>1.5.6.4</strong> — 3 февраля 2026</summary>

#### List of changes

- Добавлены 2 новых `Action`
- Изменена лицензия

</details>

<details>
<summary><strong>1.5.6.3</strong> — 1 февраля 2026</summary>

#### List of changes

- Добавлена блокировка для `NeoIniReader.Dispose`
- Добавлен предупреждающий заголовок в INI-файл

</details>

<details>
<summary><strong>1.5.6.2</strong> — 1 февраля 2026</summary>

#### List of changes

- Удалены избыточные асинхронные методы:
  - `SectionExistsAsync`
  - `KeyExistsAsync`
  - `GetAllSectionsAsync`
  - `GetAllKeysAsync`
  - `GetSectionAsync`
  - `FindKeyInAllSectionsAsync`
  - `SearchAsync`
  - `ReloadFromFileAsync`
  - `DeleteFileAsync`
  - `DeleteFileWithDataAsync`
  - `GetEncryptionPasswordAsync`

</details>

<details>
<summary><strong>1.5.6.1</strong> — 1 февраля 2026</summary>

#### List of changes

- Незначительные исправления

</details>

<details>
<summary><strong>1.5.6</strong> — 1 февраля 2026</summary>

#### List of changes

- Добавлены `Action` для всех событий
- Добавлен метод `GetEncryptionPasswordAsync`

</details>

<details>
<summary><strong>1.5.5</strong> — 1 февраля 2026</summary>

#### List of changes

- Добавлены новые методы API:
  - `GetSection`
  - `GetSectionAsync`
  - `FindKeyInAllSections`
  - `FindKeyInAllSectionsAsync`
  - `ClearSection`
  - `ClearSectionAsync`
  - `RenameKey`
  - `RenameKeyAsync`
  - `RenameSection`
  - `RenameSectionAsync`
  - `Search`
  - `SearchAsync`
- Поддержка .NET 6.0
- Добавлена иконка

</details>

<details>
<summary><strong>1.5.4.4</strong> — 1 февраля 2026</summary>

#### List of changes

- Исправление: добавлен `FileProvider = new(...)` в конструктор `NeoIniReader(string path, string encryptionPassword)`

</details>

<details>
<summary><strong>1.5.4.3</strong> — 1 февраля 2026</summary>

#### List of changes

- Добавлен возврат к резервному файлу в случае ошибки чтения

</details>

<details>
<summary><strong>1.5.4.2</strong> — 1 февраля 2026</summary>

#### List of changes

- Добавлен `IDisposable` и сохранение состояния при вызове `Dispose`

</details>

<details>
<summary><strong>1.5.4.1</strong> — 1 февраля 2026</summary>

#### List of changes

- Переименование `NeoIni` в `NeoIniReader`

</details>

<details>
<summary><strong>1.5.4</strong> — 1 февраля 2026</summary>

#### List of changes

- Создание подкласса `NeoIni`

</details>

<details>
<summary><strong>1.5.3</strong> — 31 января 2026</summary>

#### List of changes

- Добавлены новые методы API
- Удалён мусорный код и добавлена XML-документация

</details>

<details>
<summary><strong>1.5.2</strong> — 31 января 2026</summary>

#### List of changes

- Добавлены асинхронные методы и кэширование ключа шифрования

</details>

<details>
<summary><strong>1.5.1</strong> — 30 января 2026</summary>

#### List of changes

- Удалён мусорный код

</details>

<details>
<summary><strong>1.5</strong> — 30 января 2026</summary>

#### List of changes

- Полностью переработан класс
- Добавлены потокобезопасность и стабильность; улучшена философия «чёрного ящика»

</details>

</details>

<details>
<summary><strong>Версии до 1.5</strong> — древняя история. Это, по сути, другой продукт, который разделяет только репозиторий и изначальную идею. Архитектура, поверхность API, формат файлов и стандарты качества несопоставимы с тем, что есть выше. Приведены здесь только для полноты картины</summary>

<details>
<summary><strong>1.4</strong> — 17 февраля 2025</summary>

#### List of changes

- Переработка

</details>

<details>
<summary><strong>1.3.1</strong> — 12 мая 2024</summary>

#### List of changes

- Улучшения стабильности
- Автор: Fatalan

</details>

<details>
<summary><strong>1.3</strong> — 22 апреля 2024</summary>

#### List of changes

- Улучшения стабильности

</details>

<details>
<summary><strong>1.2</strong> — 20 апреля 2024</summary>

#### List of changes

- Добавлена возможность чтения данных со значением по умолчанию, которое будет возвращено в случае ошибки

</details>

<details>
<summary><strong>1.1</strong> — 20 апреля 2024</summary>

#### List of changes

- Добавлена возможность проверки существования секции
- Добавлена возможность проверки существования ключа в конкретной секции

</details>

<details>
<summary><strong>1.0</strong> — 19 апреля 2024</summary>

#### List of changes

- Выпущен INIReader

</details>

</details>
