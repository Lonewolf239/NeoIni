# Security Policy

## Supported Versions

Currently, only the latest release (**1.7.3**) is supported with security updates and patches.

Please note that **all previous versions** (`< 1.6.1`) are deprecated. They are no longer available for download as they contain known bugs or were incomplete pre-release builds that did not reach final stability standards.

**CSR** — actively maintained with new features and updates.
**LSR** — fully stable and safe to use; no further updates or new features planned.
**DSR** — deprecated and security‑risk; known to contain bugs or incomplete features, must not be used in production.

| Version | Supported | State | Notes |
|---------|-----------|-------|-------|
| 1.7.3 | :white_check_mark: | *CSR* | Current Stable Release |
| 1.7.2 | :package: | *LSR* | Legacy Stable Release |
| 1.7.1 | :package: | **LSR** | Using the weaker Set methods is not recommended if you accept untrusted input. |
| 1.7 | :package: | **LSR** | Using the weaker Set methods is not recommended if you accept untrusted input. |
| 1.6.1 | :package: | **LSR** | Using the weaker Set methods is not recommended if you accept untrusted input. |
| < 1.6.1 | :x: | **DSR** | Incomplete, buggy, or deprecated versions. |

## Reporting a Vulnerability

If you discover a security vulnerability in the supported version, please report it immediately.

*   **How to report**: Please open a Draft Security Advisory in the repository or contact us via Telegram: [@an1onime](https://t.me/an1onime).
*   **Timeline:** We aim to acknowledge all reports within 48 hours.
*   **Policy:** Please do not disclose the vulnerability publicly until a patch has been released.
