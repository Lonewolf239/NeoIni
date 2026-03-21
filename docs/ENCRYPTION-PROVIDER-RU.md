[![EN](https://img.shields.io/badge/ENCRYPTION_PROVIDER-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ENCRYPTION-PROVIDER.md)
[![RU](https://img.shields.io/badge/ENCRYPTION_PROVIDER-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ENCRYPTION-PROVIDER-RU.md)

## Pluggable encryption (2.0+)

Отделите логику шифрования от хранилища конфигурации. Интерфейс `IEncryptionProvider` позволяет подключать пользовательские алгоритмы шифрования (например, AES‑GCM, пользовательский XOR, ключи из аппаратного хранилища), сохраняя единый API `NeoIniDocument`.

---

### Using a custom encryption provider

```csharp
using NeoIni;
using NeoIni.Providers;

// Передайте свой провайдер в один из конструкторов документа
NeoIniDocument doc = new("config.ini", myEncryptionProvider);

// Или используйте асинхронную фабрику
NeoIniDocument doc = await NeoIniDocument.CreateAsync("config.ini", myEncryptionProvider, options: null, autoLoad: true);
```

Провайдер используется для получения ключей и соли каждый раз при чтении или записи файла.

---

### Implementing IEncryptionProvider

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;
using NeoIni.Providers;

public class MyAesGcmEncryptionProvider : IEncryptionProvider
{
    public EncryptionParameters GetEncryptionParameters(string? password = null, byte[]? salt = null)
    {
        // Сгенерируйте или выведите ключ (например, из пароля и соли с помощью KDF)
        // Верните новый EncryptionParameters(byte[] key, byte[] salt)
    }

    public string GetEncryptionPassword(byte[]? salt)
    {
        // Верните пароль, который будет использоваться для вывода ключа для данной соли.
        // Этот метод вызывается методом GetEncryptionPassword() документа.
    }

    public void Encrypt(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes)
    {
        // Запишите вектор инициализации (16 байт) и соль (16 байт) в поток,
        // затем зашифруйте и запишите полезные данные.
        // Порядок ОБЯЗАТЕЛЕН: IV (16) + Salt (16) + EncryptedData.
        // Этот порядок требуется для совместимости со встроенным файловым провайдером.
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM; // пример
        aes.Key = key;
        aes.GenerateIV();
        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        memoryStream.Write(salt, 0, salt.Length);
        using var encryptor = aes.CreateEncryptor();
        using (var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
        }
    }

    public async Task EncryptAsync(MemoryStream memoryStream, byte[] key, byte[] salt, byte[] plaintextBytes, CancellationToken ct = default)
    {
        // Асинхронная версия – тот же порядок
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
        aes.Key = key;
        aes.GenerateIV();
        ct.ThrowIfCancellationRequested();
        await memoryStream.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length), ct).ConfigureAwait(false);
        await memoryStream.WriteAsync(salt, ct).ConfigureAwait(false);
        using var encryptor = aes.CreateEncryptor();
        await using (var cs = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length, ct).ConfigureAwait(false);
            await cs.FlushFinalBlockAsync(ct).ConfigureAwait(false);
        }
    }

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] encryptedBytes)
    {
        // Расшифровка с использованием ключа и IV (соль уже использована при выводе ключа)
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
        aes.Key = key;
        aes.IV = iv;
        using var ms = new MemoryStream(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    public async Task<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] encryptedBytes, CancellationToken ct = default)
    {
        // Асинхронная версия
        using var aes = Aes.Create();
        aes.Mode = CipherMode.GCM;
        aes.Key = key;
        aes.IV = iv;
        ct.ThrowIfCancellationRequested();
        using var ms = new MemoryStream(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        await cs.CopyToAsync(result, ct).ConfigureAwait(false);
        return result.ToArray();
    }
}
```

- `GetEncryptionParameters` вызывается при создании нового зашифрованного файла или чтении существующего (в авто‑режиме). Он получает опциональные пароль и соль. Если оба равны null, провайдер должен сгенерировать случайную соль и пароль по умолчанию (например, на основе окружения).
- `GetEncryptionPassword` вызывается методом `GetEncryptionPassword()` для получения пароля, соответствующего заданной соли (например, для миграции).
- `Encrypt`/`EncryptAsync` должны записывать IV и соль в поток **именно** в описанном порядке. Файловый провайдер считывает их в том же порядке.
- `Decrypt`/`DecryptAsync` получают извлечённый IV и зашифрованные данные (без IV и соли). Должны возвращать расшифрованные байты.

> **Важно:** Порядок записи — **IV (16 байт) + Salt (16 байт) + EncryptedData**. Такой формат ожидается `NeoIniFileProvider`. Если вы его измените, ваши файлы не будут читаться встроенным провайдером (и наоборот). Если вы используете только свой провайдер, можете использовать любой формат, но он должен быть согласован.

---

### Built-in provider

NeoIni поставляется с реализацией AES‑256‑CBC по умолчанию (`NeoIniEncryptionProvider`). Он используется автоматически в конструкторах, не принимающих пользовательский провайдер. Поддерживает два режима:

- **Авто‑шифрование:** ключ выводится из окружения текущего пользователя/машины (непереносимый).
- **На основе пароля:** ключ выводится из строки, предоставленной пользователем, через PBKDF2 (320 000 итераций).

---

### Notes

- Все файловые конструкторы (`new NeoIniDocument(path)`, `autoEncryption`, `encryptionPassword`) внутри используют встроенный провайдер. Пользовательский провайдер нужен только для замены алгоритма шифрования.
- Параметр `salt` в `GetEncryptionParameters` опционален. Если ваш провайдер не использует соль, вы можете его игнорировать.
- Возвращаемые `EncryptionParameters` должны содержать корректный ключ (не null), если шифрование включено. Если вернуть ключ null, документ выбросит `MissingEncryptionKeyException`.
- Провайдер должен быть не сохраняющим состояние и потокобезопасным, так как может вызываться одновременно из разных документов.

> **Примечание:** В версиях до 2.0 шифрование было жёстко привязано к AES‑256‑CBC. Перейдите на версию 2.0, чтобы воспользоваться новой расширяемой архитектурой.
