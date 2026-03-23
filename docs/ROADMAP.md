[![EN](https://img.shields.io/badge/ROADMAP-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./ROADMAP.md)
[![RU](https://img.shields.io/badge/ROADMAP-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./ROADMAP-RU.md)

## Roadmap · NeoIni

Long-term development plan for NeoIni. The library evolves from a single-file INI reader toward a universal, provider-based configuration framework while preserving the **Black Box** philosophy and the internal `Dictionary<string, Dictionary<string, string>>` data model.

---

### 1.7.x — Foundation

| Version | Status | Description |
|---------|--------|-------------|
| 1.7 | ✅ Released | Object mapping API (`Get<T>`, `Set<T>`) via source generator. |
| 1.7.1 | ✅ Released | Hot-reload via file watcher (polling with checksum comparison). |
| 1.7.2 | ✅ Released | Human-editable INI mode (comment preservation, no checksum). |
| 1.7.3 | ✅ Released | Pluggable provider abstraction (`INeoIniProvider`). |

---

### 1.8 — Compatibility & Hardening

| Version | Status | Description |
|---------|--------|-------------|
| 1.8 | ✅ Released | Quoted value support: `key = "value ; not a comment"`. Broadens compatibility with real-world INI files (MySQL, PHP, Git config). |

---

### 1.9 — Async Internals

| Version | Status | Description |
|---------|--------|-------------|
| 1.9 | ✅ Released | Async-safe concurrency: replace `ReaderWriterLockSlim` with async-compatible primitive. |

---

### 2.0 — Major Redesign

| Version | Status | Description |
|---------|--------|-------------|
| 2.0 | ✅ Released | Rename `NeoIniReader` → `NeoIniDocument`. Introduce `IEncryptionProvider` interface to allow pluggable encryption algorithms (AES, custom implementations). |

---

### 3.0 — Constructor Rework & EncryptionType

| Version | Status | Description |
|---------|--------|-------------|
| 3.0 | ✅ Released | Reworked constructors; added `EncryptionType` enum. |

### 3.x — Future Directions (Planned / Under Consideration)

| Version | Status | Description |
|---------|--------|-------------|
| 3.1 | ✅ Released | **Improvement of IEncryptionProvider**: Transferring encryption logic from `NeoIniFileProvider`. |
| 3.2 | ✅ Released | **Streaming provider support**: allow large configurations to be read/written incrementally without loading the entire dataset into memory. |
| 3.3 | 🔵 Under consideration | **Extended source generator**: support for nested objects, collections, and validation attributes (e.g., `[Range]`, `[Required]`) in the generated mapping code. |
| 3.4 | 🔵 Under consideration | **Memory-mapped I/O**: optionally use memory-mapped files for very large INI files to improve performance and reduce memory footprint. |
| 3.5 | 🔵 Under consideration | **Batch operations**: methods like `SetValuesAsync` to update multiple keys in a single atomic operation, reducing auto‑save overhead. |
| 3.6 | 🔵 Under consideration | **.NET Standard 2.0 support**: enable usage on .NET Framework 4.6.2+ and other legacy platforms by backporting async/await and other modern features. |
