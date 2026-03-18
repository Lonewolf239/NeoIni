[![EN](https://img.shields.io/badge/HUMAN_MODE-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./HUMAN-MODE.md)
[![RU](https://img.shields.io/badge/HUMAN_MODE-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./HUMAN-MODE-RU.md)

## Human-editable mode (1.7.2+)

Preserve comments, blank lines, and formatting in hand-edited INI files. In standard mode, NeoIni owns the file and strips non-data content. Human-editable mode disables this behavior so the file stays readable and editable by people.

---

### Creating a reader

```csharp
using NeoIni;

// Synchronous
NeoIniReader reader = NeoIniReader.CreateHumanMode("config.ini");

// Asynchronous
NeoIniReader reader = await NeoIniReader.CreateHumanModeAsync("config.ini", cancellationToken: ct);
```

---

### Behavior when enabled

- **Comments preserved:** Lines starting with `;` are kept intact across load/save cycles.
- **Original ordering:** Sections and keys remain in their original order.
- **No checksum:** `UseChecksum` is disabled — the file is not checksummed or integrity-checked.
- **No encryption:** AES-256 encryption is not available in human mode.

---

### Limitations

- Human mode is **read/write but not merge-safe** — if the file is modified externally while the reader holds unsaved changes, the external edits may be overwritten on save.
- All other `NeoIniReader` features (typed get/set, events, auto-save, sections, search) work as normal.

---

### Custom providers (1.7.3+)

Human-editable mode works with custom providers. Pass an `INeoIniProvider` instance instead of a file path:

```csharp
NeoIniReader reader = NeoIniReader.CreateHumanMode(myCustomProvider);
```

---

> **Experimental.** Human-editable mode is under active development. Behavior may change in future releases.
