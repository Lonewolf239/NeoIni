## Attribute‑based mapping & Source Generator (1.7+)

Начиная с версии **1.7**, NeoIni поставляется с:

- NeoIni.Annotations.NeoIniKeyAttribute — атрибутом для свойств конфигурационных классов.
- Roslyn source generator (NeoIni.Generators.NeoIniMappingGenerator), который генерирует:
  - `NeoIni.NeoIniReaderExtensions.Get<T>(this NeoIniReader reader) where T : new()`
  - `NeoIni.NeoIniReaderExtensions.Set<T>(this NeoIniReader reader, T config)`

Это позволяет описывать конфигурацию как обычный C#‑класс и мапить его в INI без ручного копирования/вставки вызовов `GetValue`/`SetValue`.

### Defining a config model

Используйте NeoIniKeyAttribute, чтобы задать маппинг.
Атрибут принимает два обязательных параметра конструктора (Section и Key) и одно опциональное свойство (DefaultValue).

```csharp
using NeoIni.Annotations;

namespace MyApp.Config;

public sealed class AppConfig
{
    // Обязательные аргументы конструктора: Section ("General"), Key ("AppName")
    // Необязательное свойство: DefaultValue ("MyApp")
    [NeoIniKey("General", "AppName", DefaultValue = "MyApp")]
    public string AppName { get; set; }

    [NeoIniKey("General", "LogLevel", DefaultValue = "Info")]
    public string LogLevel { get; set; }

    [NeoIniKey("Database", "Host", DefaultValue = "localhost")]
    public string DbHost { get; set; }

    [NeoIniKey("Database", "Port", DefaultValue = "5432")]
    public int DbPort { get; set; }

    [NeoIniKey("Features", "EnableMetrics", DefaultValue = "true")]
    public bool EnableMetrics { get; set; }
}
```

### Read the entire configuration

Source Generator автоматически создаёт высокоэффективный extension‑метод `Get<T>()`.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniReader reader = new("config.ini");

// Создаёт экземпляр AppSettings и заполняет его значениями из INI.
// Отсутствующие ключи используют DefaultValue из атрибута (или стандартные значения).
AppSettings settings = reader.Get<AppSettings>();

Console.WriteLine($"App: {settings.AppName}, DB: {settings.DbHost}:{settings.DbPort}");
```

### Save the entire configuration

Используйте сгенерированный метод `Set<T>()`, чтобы записать все помеченные свойства обратно в INI‑файл.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniReader reader = new("config.ini");

AppSettings newSettings = new()
{
    AppName = "My Super App",
    LogLevel = "Debug",
    DbHost = "192.168.1.100",
    DbPort = 5432,
    EnableMetrics = false
};

// Записывает все свойства, помеченные [NeoIniKey], в reader
reader.Set(newSettings);

// Сохраняем изменения на диск
reader.SaveFile();
```

> **Note:** Source generator во время компиляции превращает атрибуты в безопасные, строго типизированные вызовы `GetValue<T>` и `SetValue<T>`, так что в runtime нет **никакого reflection‑оверхода**.
