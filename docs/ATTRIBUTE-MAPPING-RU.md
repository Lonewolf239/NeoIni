[![EN](https://img.shields.io/badge/ATTRIBUTE_MAPPING-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ATTRIBUTE-MAPPING.md)
[![RU](https://img.shields.io/badge/ATTRIBUTE_MAPPING-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ATTRIBUTE-MAPPING-RU.md)

## Attribute-based mapping & source generator (1.7+)

Маппинг INI-конфигурации на типизированные C#-классы без накладных расходов на рефлексию. Source generator NeoIni транслирует аннотации `NeoIniKeyAttribute` в extension-методы `Get<T>()` / `Set<T>()` на этапе компиляции — ручные вызовы `GetValue`/`SetValue` не нужны.

---

### Defining a config model

Пометьте свойства атрибутом `NeoIniKeyAttribute`. Атрибут принимает два обязательных параметра конструктора (`Section`, `Key`) и одно необязательное свойство (`DefaultValue`).

#### Syntax changes between versions

- **NeoIni 1.8 и старше** – значение по умолчанию задаётся через именованное свойство `DefaultValue`:
```csharp
[NeoIniKey("General", "AppName", DefaultValue = "MyApp")]
```

```csharp
using NeoIni.Annotations;

namespace MyApp.Config;

public sealed class AppConfig
{
    [NeoIniKey("General", "AppName", "MyApp")]
    public string AppName { get; set; }

    [NeoIniKey("General", "LogLevel", "Info")]
    public string LogLevel { get; set; }

    [NeoIniKey("Database", "Host", "localhost")]
    public string DbHost { get; set; }

    [NeoIniKey("Database", "Port", 5432)]
    public int DbPort { get; set; }

    [NeoIniKey("Features", "EnableMetrics", true)]
    public bool EnableMetrics { get; set; }
}
```

---

### Reading

Source generator создаёт extension-метод `Get<T>()`, который инстанцирует модель и заполняет её значениями из INI-файла. Отсутствующие ключи используют `DefaultValue`, указанный в атрибуте.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniDocument document = new("config.ini");

AppConfig config = document.Get<AppConfig>();

Console.WriteLine($"App: {config.AppName}, DB: {config.DbHost}:{config.DbPort}");
```

---

### Writing

Используйте `Set<T>()` для записи всех маппированных свойств обратно в INI-файл.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniDocument document = new("config.ini");

AppConfig config = new()
{
    AppName = "My Super App",
    LogLevel = "Debug",
    DbHost = "192.168.1.100",
    DbPort = 5432,
    EnableMetrics = false
};

document.Set(config);
document.SaveFile();
```

---

> **Нулевая рефлексия в рантайме.** Source generator генерирует строго типизированные вызовы `GetValue<T>` и `SetValue<T>` на этапе компиляции — рефлексия в рантайме не используется.
