## Pluggable Providers (1.7.3+)

Начиная с **1.7.3**, `NeoIniReader` работает через интерфейс `INeoIniProvider`, а не напрямую с файловой системой. Вы можете реализовать собственный провайдер для хранения конфигурации в базе данных, удалённом сервисе, в памяти или любом другом бэкенде.

### Using a custom provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Синхронно
NeoIniReader reader = new(myCustomProvider);

// Асинхронно
NeoIniReader reader = await NeoIniReader.CreateAsync(myCustomProvider, cancellationToken: ct);

// Human mode с кастомным провайдером
NeoIniReader reader = NeoIniReader.CreateHumanMode(myCustomProvider);
```

### Implementing INeoIniProvider

```csharp
using NeoIni.Internal;
using NeoIni.Providers;

public class MyDatabaseProvider : INeoIniProvider
{
    public event EventHandler<ProviderErrorEventArgs> Error;
    public event EventHandler<ChecksumMismatchEventArgs> ChecksumMismatch;

    public NeoIniData GetData(bool humanization = false)
    {
        // Загрузите данные из вашего хранилища и верните распарсенные секции + комментарии
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

### Notes

- Все существующие файловые конструкторы (`new NeoIniReader(path)`, варианты с шифрованием) продолжают работать как раньше — внутри они используют встроенный `NeoIniFileProvider`.
- `UseAutoBackup`, `DeleteFile`, `DeleteBackup` и `GetEncryptionPassword` специфичны для файлового провайдера. При вызове на кастомном провайдере будет выброшено `UnsupportedProviderOperationException`.
- Hot-reload работает с любым провайдером, который возвращает осмысленное значение из `GetStateChecksum()`.
