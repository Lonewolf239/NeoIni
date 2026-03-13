## Attribute‑based mapping & Source Generator (1.7+)

Starting from **1.7**, NeoIni ships with:

- NeoIni.Annotations.NeoIniKeyAttribute — an attribute assigned to properties of configuration classes.
- Roslyn source generator (NeoIni.Generators.NeoIniMappingGenerator), which generates:
  - `NeoIni.NeoIniReaderExtensions.Get<T>(this NeoIniReader reader) where T : new()`
  - `NeoIni.NeoIniReaderExtensions.Set<T>(this NeoIniReader reader, T config)`

This allows you to describe the configuration as a regular C# class and map it to INI without manually copying and pasting `GetValue`/`SetValue`.

### Defining a config model

Use the NeoIniKeyAttribute to specify the mapping.
The attribute takes two required constructor parameters (Section and Key) and one optional property (DefaultValue).

```csharp
using NeoIni.Annotations;

namespace MyApp.Config;

public sealed class AppConfig
{
    // Required constructor args: Section ("General"), Key ("AppName")
    // Optional property: DefaultValue ("MyApp")
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

The Source Generator automatically creates a highly optimized `Get<T>()` extension method.

```csharp
using NeoIni;
using MyApp.Config;

NeoIniReader reader = new("config.ini");

// Instantiates AppSettings and populates it with values from the INI file.
// Missing keys will use the DefaultValue from the attribute (or standard defaults).
AppSettings settings = reader.Get<AppSettings>();

Console.WriteLine($"App: {settings.AppName}, DB: {settings.DbHost}:{settings.DbPort}");
```

### Save the entire configuration

Use the generated `Set<T>()` method to write all mapped properties back to the INI file.

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

// Writes all [NeoIniKey] mapped properties to the reader
reader.Set(newSettings);

// Save changes to disk
reader.SaveFile();
```

> **Note:** The source generator translates your attributes directly into safe, strongly-typed `GetValue<T>` and `SetValue<T>` calls during compilation, meaning there is **zero reflection overhead** at runtime.
