[![EN](https://img.shields.io/badge/HUMAN_MODE-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./HUMAN-MODE.md)
[![RU](https://img.shields.io/badge/HUMAN_MODE-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./HUMAN-MODE-RU.md)

## Human-editable mode (1.7.2+)

Сохранение комментариев, пустых строк и форматирования в INI-файлах, редактируемых вручную. В стандартном режиме NeoIni владеет файлом и удаляет не-данные. Human-editable mode отключает это поведение, позволяя файлу оставаться читаемым и редактируемым людьми.

---

### Creating a reader

```csharp
using NeoIni;

// Синхронно
NeoIniReader reader = NeoIniReader.CreateHumanMode("config.ini");

// Асинхронно
NeoIniReader reader = await NeoIniReader.CreateHumanModeAsync("config.ini", cancellationToken: ct);
```

---

### Behavior when enabled

- **Комментарии сохраняются:** Строки, начинающиеся с `;`, остаются нетронутыми при циклах загрузки/сохранения.
- **Исходный порядок:** Секции и ключи остаются в оригинальном порядке.
- **Без checksum:** `UseChecksum` отключён — файл не проверяется на целостность.
- **Без шифрования:** AES-256 шифрование недоступно в human mode.

---

### Limitations

- Human mode поддерживает **чтение и запись, но не безопасное слияние** — если файл изменён извне, пока reader содержит несохранённые изменения, внешние правки могут быть перезаписаны при сохранении.
- Все остальные возможности `NeoIniReader` (типизированные get/set, события, auto-save, секции, поиск) работают как обычно.

---

### Custom providers (1.7.3+)

Human-editable mode работает с пользовательскими provider-ами. Передайте экземпляр `INeoIniProvider` вместо пути к файлу:

```csharp
NeoIniReader reader = NeoIniReader.CreateHumanMode(myCustomProvider);
```

---

> **Экспериментальный режим.** Human-editable mode находится в активной разработке. Поведение может измениться в будущих версиях.
