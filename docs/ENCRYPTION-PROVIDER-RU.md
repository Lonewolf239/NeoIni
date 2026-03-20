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
}
```

- `GetEncryptionParameters` вызывается при создании нового зашифрованного файла или чтении существующего (в авто‑режиме). Он получает опциональные пароль и соль. Если оба равны null, провайдер должен сгенерировать случайную соль и пароль по умолчанию (например, на основе окружения).
- `GetEncryptionPassword` вызывается методом `GetEncryptionPassword()` для получения пароля, соответствующего заданной соли (например, для миграции).

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