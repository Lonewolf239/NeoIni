[![EN](https://img.shields.io/badge/SECURITY-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./SECURITY.md)
[![RU](https://img.shields.io/badge/SECURITY-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./SECURITY-RU.md)

## Security policy · NeoIni

Report vulnerabilities responsibly so they can be fixed before public disclosure.

---

### Supported versions

NeoIni follows a tiered support model:

| Abbreviation | Meaning | Scope |
|--------------|---------|-------|
| **CSR** | Current Stable Release | Full security patches and bug fixes |
| **LSR** | Long-term Stable Release | No patches — available for download only |
| **DSR** | Deprecated Stable Release | No patches — upgrade strongly recommended |

| Version | Status | Support |
|---------|--------|---------|
| 3.2.2 | **CSR** | Security patches + bug fixes |
| 3.2.1 | LSR | Available for download |
| 3.2 | LSR | Available for download |
| 3.1 | LSR | Available for download |
| 3.0 | LSR | Available for download |
| 2.x, 1.x and earlier | **DSR** | Incomplete, buggy, or deprecated versions. If you are using versions older than 2.0, please upgrade to 3.2.1. |

---

### Reporting a vulnerability

**Do not open a public issue for security vulnerabilities.**

Contact the maintainer directly:

- **Telegram:** [@an1onime](https://t.me/an1onime)

Include in your report:

1. **Affected version(s).**
2. **Description** of the vulnerability and its impact.
3. **Reproduction steps** — minimal code or configuration to trigger the issue.
4. **Suggested fix** (if you have one).

You will receive an acknowledgment within **48 hours**. A fix will be developed privately and released as a patch before public disclosure.

---

### Scope

The following areas are in scope for security reports:

- AES-256 encryption (key derivation, IV/salt handling, CBC mode).
- SHA-256 checksum validation and `.backup` fallback logic.
- File I/O race conditions (atomic writes, temp files).
- Thread-safety issues under concurrent access.
- Provider interface — data leakage or bypass via custom `INeoIniProvider` implementations.

Out of scope: denial-of-service via intentionally malformed INI files with extreme sizes.
