[![EN](https://img.shields.io/badge/HOT_RELOAD-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./HOT-RELOAD.md)
[![RU](https://img.shields.io/badge/HOT_RELOAD-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./HOT-RELOAD-RU.md)

## Hot-reload (1.7.1+)

Автоматическое обнаружение изменений конфигурации и перезагрузка данных без перезапуска приложения. NeoIni использует polling с проверкой checksum для отслеживания внешних модификаций INI-источника.

---

### Starting and stopping

```csharp
using NeoIni;

NeoIniReader reader = new("config.ini");

// Запуск hot-reload с polling каждые 2 секунды
reader.StartHotReload(2000);

// Остановка отслеживания
reader.StopHotReload();
```

`StartHotReload(int delayMs)` принимает интервал polling в миллисекундах. Минимум — **1000 мс**, меньшие значения вызовут `InvalidHotReloadDelayException`. Вызовите `StopHotReload()` для отключения в любой момент.

---

### Behavior and safety

- **Polling + checksum:** На каждом интервале NeoIni вычисляет checksum текущего состояния источника и сравнивает его с последним известным значением. Если checksum отличается, данные перезагружаются.
- **Потокобезопасность:** Перезагрузка захватывает внутренний write lock `ReaderWriterLockSlim`, поэтому конкурентное чтение безопасно.
- **События:** Успешная перезагрузка вызывает событие `Loaded`. При ошибке срабатывает событие `Error`, а предыдущие данные сохраняются.
- **Взаимодействие с AutoSave:** Если включён `UseAutoSave`, изменения в памяти сохраняются перед проверкой перезагрузки, предотвращая потерю данных.

---

### Provider support

Начиная с **1.7.3**, hot-reload работает с любым `INeoIniProvider`, который возвращает осмысленное значение из `GetStateChecksum()`. Встроенный `NeoIniFileProvider` использует временну́ю метку последней записи и размер файла в качестве checksum. Пользовательские provider-ы могут возвращать любой массив байт, изменяющийся при изменении данных.
