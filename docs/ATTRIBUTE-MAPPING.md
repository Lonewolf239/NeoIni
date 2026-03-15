## Attribute-based mapping & source generator (1.7+)

Map INI configuration to typed C# classes with zero reflection overhead. NeoIni's source generator translates `NeoIniKeyAttribute` annotations into compile-time `Get<T>()` / `Set<T>()` extension methods — no manual `GetValue`/`SetValue` calls required.

---

### Defining a config model

Decorate properties with `NeoIniKeyAttribute`. The attribute takes two required constructor parameters (`Section`, `Key`) and one optional property (`DefaultValue`).

```csharp
using NeoIni.Annotations;

namespace MyApp.Config;

public sealed class AppConfig
{
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

---

### Reading

The source generator creates a `Get<T>()` extension method that instantiates the model and populates it from the INI file. Missing keys fall back to the `DefaultValue` specified in the attribute.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniReader reader = new("config.ini");

AppConfig config = reader.Get<AppConfig>();

Console.WriteLine($"App: {config.AppName}, DB: {config.DbHost}:{config.DbPort}");
```

---

### Writing

Use `Set<T>()` to write all mapped properties back to the INI file.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniReader reader = new("config.ini");

AppConfig config = new()
{
    AppName = "My Super App",
    LogLevel = "Debug",
    DbHost = "192.168.1.100",
    DbPort = 5432,
    EnableMetrics = false
};

reader.Set(config);
reader.SaveFile();
```

---

> **Zero reflection at runtime.** The source generator emits strongly-typed `GetValue<T>` and `SetValue<T>` calls at compile time — no reflection is used at runtime.
