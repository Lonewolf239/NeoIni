[![EN](https://img.shields.io/badge/PROVIDERS-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./PROVIDERS.md)
[![RU](https://img.shields.io/badge/PROVIDERS-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./PROVIDERS-RU.md)

## Pluggable providers (1.7.3+)

Отделение хранилища конфигурации от файловой системы. Интерфейс `INeoIniProvider` позволяет подключить `NeoIniDocument` к базе данных, удалённому сервису, хранилищу в памяти или любому пользовательскому бэкенду.

---

### Using a custom provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Синхронно
NeoIniDocument document = new(myCustomProvider);

// Асинхронно
NeoIniDocument document = await NeoIniDocument.CreateAsync(myCustomProvider, cancellationToken: ct);

// Human mode с пользовательским provider-ом
NeoIniDocument document = NeoIniDocument.CreateHumanMode(myCustomProvider);
```

---

### Implementing INeoIniProvider

```csharp
using NeoIni.Models;
using NeoIni.Providers;

public class MyDatabaseProvider : INeoIniProvider
{
    public event EventHandler<ProviderErrorEventArgs> Error;
    public event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch;

    public NeoIniData GetData(bool humanization = false)
    {
        // Загрузите данные из хранилища и верните распарсенные секции + комментарии
    }

    public Task<NeoIniData> GetDataAsync(bool humanization = false, CancellationToken ct = default)
    {
        // Асинхронная версия GetData
    }

    public void Save(string content, bool useChecksum)
    {
        // Сохраните сериализованное INI-содержимое
    }

    public Task SaveAsync(string content, bool useChecksum, CancellationToken ct = default)
    {
        // Асинхронная версия Save
    }

    public byte[] GetStateChecksum()
    {
        // Верните хеш текущего состояния для обнаружения изменений (hot-reload),
        // или null, если не поддерживается
    }

    public void RaiseError(object sender, ProviderErrorEventArgs e)
        => Error?.Invoke(sender ?? this, e);
}
```

---

### Notes

- Все существующие файловые конструкторы (`new NeoIniDocument(path)`, варианты с шифрованием) продолжают работать — внутри они используют встроенный `NeoIniFileProvider`.
- `UseAutoBackup`, `DeleteFile`, `DeleteBackup` и `GetEncryptionPassword` специфичны для файлового provider-а. При вызове на пользовательском provider-е будет выброшено `UnsupportedProviderOperationException`.
- Hot-reload работает с любым provider-ом, который возвращает осмысленное значение из `GetStateChecksum()`.
