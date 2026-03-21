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
