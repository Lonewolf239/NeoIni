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
| 1.8 | 🕓 Planned | Quoted value support: `key = "value ; not a comment"`. Broadens compatibility with real-world INI files (MySQL, PHP, Git config). |

---

### 1.9 — Async Internals

| Version | Status | Description |
|---------|--------|-------------|
| 1.9 | 🕓 Planned | Async-safe concurrency: replace `ReaderWriterLockSlim` with async-compatible primitive. Eliminate sync/async code duplication via unified async core. |

---

### 2.0 — Major Redesign

| Version | Status | Description |
|---------|--------|-------------|
| 2.0 | 🕓 Planned | Rename `NeoIniReader` → `NeoIniDocument`. Extract `HotReloadManager` into a standalone component. |

#### Migration strategy

| Step | Detail |
|------|--------|
| 2.0-beta | Ship `NeoIniDocument` alongside `NeoIniReader` marked `[Obsolete]`. Both classes share the same internal engine. |
| 2.0 | Remove `NeoIniReader`. Provide a one-line migration guide in release notes (`NeoIniReader` → `NeoIniDocument`, no API changes beyond the rename). |

---

### Cross-cutting (ongoing)

| Area | Description |
|------|-------------|
| Tests | Maintain ≥ 90 % line coverage. Add integration test suite for each `INeoIniProvider` implementation. Benchmark suite (BenchmarkDotNet) tracking parse, save, and hot-reload performance across releases. |
| CI/CD | GitHub Actions pipeline: build → test → benchmark → NuGet publish. |
| Documentation | XML-doc on every public member. [README](./README.md) quick-start kept in sync with latest release. Full migration guide published with every major version. |
| Examples | `NeoIniDemo` project covering: file creation, sections, keys/values, clamp & auto-add, search & rename, options & presets, encryption & password migration, async operations, auto-features (auto-save, hot-reload), file error recovery, events, read-only & performance modes, and attribute-based source generator mapping. |
