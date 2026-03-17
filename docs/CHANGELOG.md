[![EN](https://img.shields.io/badge/CHANGELOG-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CHANGELOG.md)
[![RU](https://img.shields.io/badge/CHANGELOG-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CHANGELOG-RU.md)

## Changelog · NeoIni

### 1.9-pre1 — March 17, 2026

#### List of changes

- `ReaderWriterLock` replaced with `AsyncReaderWriterLock`

### 1.8 — March 16, 2026

#### List of changes

- Added missing `ConfigureAwait(false)` calls.
- Added a `CancellationToken` parameter to the `FinalizeSave` method.
- Improved invalid input handling in `Set` methods.

### 1.8-pre1 — March 15, 2026

#### List of changes

- Fixed the display of escaping in the `Search` method.
- Eliminated unnecessary memory allocations.
- Added optional support for quoted values via the `UseShielding` parameter (e.g., `key = "value ; not a comment"`).
- Refactored code to remove duplication.

### 1.7.3 — March 15, 2026

#### List of changes

- Added INeoIniProvider interface for pluggable storage backends (database, remote, in-memory, etc.)
- Implemented NeoIniFileProvider that encapsulates all existing file-based logic
- Added NeoIniReader constructors accepting INeoIniProvider (sync, async, human mode)
- Introduced UnsupportedProviderOperationException for file-specific operations on custom providers
- Guarded UseAutoBackup in ApplyOptions to prevent crashes on non-file providers
- Made NeoIniData public to allow custom provider implementations

### 1.7.2 — March 13, 2026

#### List of changes

- Fixed a vulnerability when passing values to `Set` methods

### 1.7.2-pre1 — March 13, 2026

#### List of changes

- Added human‑editable INI mode (HumanMode) with automatic comments preserving and re‑emitting
- Introduced optional "humanization" pipeline that keeps comment positions relative to sections and keys
- Disabled checksum validation automatically when HumanMode is enabled to allow manual edits
- Prevented using HumanMode together with encryption to avoid data corruption and UX pitfalls

### 1.7.1 — March 13, 2026

#### List of changes

- Added hot-reload functionality via file watcher.

### 1.7 — March 12, 2026

#### List of changes

- Removed leftover and unused code from NeoIniMappingGenerator to simplify the source generator implementation.

### 1.7-pre1 — March 12, 2026

#### List of changes

- Added NeoIni.Annotations mini-package with NeoIniKeyAttribute for mapping model properties to specific INI sections and keys, including an optional DefaultValue.
- Added NeoIni.Generators mini-package with NeoIniMappingGenerator (IIncrementalGenerator) that scans properties annotated with NeoIniKeyAttribute and generates NeoIniReaderExtensions containing strongly-typed Get / Set object mapping APIs.
- Generated NeoIniReaderExtensions.Get creates and populates configuration instances from INI files using GetValue for each mapped property, applying per-property defaults or type defaults when values are missing.
- Generated NeoIniReaderExtensions.Set writes configuration instances back to INI files via SetValue, throwing NotSupportedException for types not covered by the source generator's mapping.

### 1.6.1 — March 12, 2026

#### List of changes

- Fixed escaping of backslash (`\`) characters in the parser to prevent corruption of Windows file paths
- Fixed an issue where saving an entirely empty configuration (all sections removed) was ignored
- Fixed parsing logic in `TryMatchKey` to correctly load keys with empty values when AllowEmptyValues is enabled
- Added null safety (`?? string.Empty`) in `SaveFile` and `SaveFileAsync` to prevent `ArgumentNullException`
- Improved `CryptoStream` memory management by setting `leaveOpen: true` to prevent premature stream disposal

### 1.6.1-pre2 — March 12, 2026

#### List of changes

- Fixed minimum file length calculation in `NeoIniFileProvider` by including `SaltSize`
- Implemented auto-password retrieval fallback when opening an auto-encrypted file with a custom password

### 1.6.1-pre1 — March 12, 2026

#### List of changes

- Refactored `Action` delegates to use `EventHandler`
- Added the `AllowEmptyValues` field to manage empty data states
- Improved overall code optimization and execution performance
- Introduced new clamped API methods:
  - `AddKeyClamped<T>` (`AddKeyClampedAsync<T>`)
  - `SetValueClamped<T>` (`SetValueClampedAsync<T>`)
- Renamed API methods for consistency: `AddKeyInSection` -> `AddKey` (including async variants) and `SetKey` -> `SetValue` (including async variants)

### 1.6.0.1 — March 6, 2026

#### List of changes

- Fixed assignment of the hasSalt flag

### 1.6 — March 6, 2026

#### List of changes

- Reworked encryption pipeline to use PBKDF2 with a unique per-file salt instead of salt derived from the password
- Embedded random 16-byte salt alongside IV in the file format for encrypted configurations
- Updated key derivation for both auto-encryption (environment-based) and custom password modes
- Introduced NeoIniIO helper class to centralize buffered file read/write operations
- Reimplemented async file IO using true asynchronous FileStream operations with cancellation support
- Added async data loading path in NeoIniFileProvider and NeoIniReader (database/config load)
- Implemented CreateAsync factory overloads (plain, auto-encrypted, password-encrypted) for non-blocking initialization
- Fixed and unified async variants for core public API methods (GetValueAsync, SetKeyAsync, section/key operations)
- Improved thread-safety and cancellation handling across async methods (lock usage + CancellationToken checks)

---

> **1.5.x and below** — early active development. The library was being shaped: APIs changed frequently, bugs were common, and many releases were incremental fixes rather than planned features. Stable usage starts from 1.6.1.

### 1.5.8.2 — March 5, 2026

#### List of changes

- Added NeoIniReaderOptions to configure NeoIniReader behavior, including predefined presets

### 1.5.8.1 — March 5, 2026

#### List of changes

- Improve newline escaping logic

### 1.5.8 — March 5, 2026

#### List of changes

- Reworked NeoIniFileProvider:
  - Added a file information header.
  - Implemented automatic decryption for encrypted files when reading, if the instance is created with encryption disabled and the file is encrypted in automode.
  - Improved file reading logic: file data is now always read correctly, regardless of how the file is opened (previously, opening a file with a checksum but without specifying the checksum could cause an error).
- Added NeoIniReader.ToString() method.
- Removed redundant and unused code.

### 1.5.7.9 — March 5, 2026

#### List of changes

- Added disabling warning when disabling checksum
- Fixed asynchronous saving
- Fixed escaping of line breaks in values

### 1.5.7.8 — February 10, 2026

#### List of changes

- Added support for multiline

### 1.5.7.7 — February 5, 2026

#### List of changes

- Fixed file reading

### 1.5.7.6 — February 5, 2026

#### List of changes

- Dispose method has been finally fixed

### 1.5.7.5 — February 5, 2026

#### List of changes

- Dispose method has been fixed

### 1.5.7.4 — February 5, 2026

#### List of changes

- Improved file reading/writing logic
- Moved code from NeoIniReader to NeoIniReaderCore

### 1.5.7.3 — February 4, 2026

#### List of changes

- The same logic has been moved to separate methods
- A read error when disabling `UseChecksum` has been fixed

### 1.5.7.2 — February 4, 2026

#### List of changes

- Added `NeoIniReader.GetValueClamp<T>`
- Changed the warning message in the file

### 1.5.7.1 — February 4, 2026

#### List of changes

- Thread safety has been reworked
- A critical thread safety bug has been fixed

### 1.5.7 — February 4, 2026

#### List of changes

- Replaced object Lock with ReaderWriterLockSlim for improved thread safety and concurrent read access
- Implemented proper IDisposable pattern with Dispose(bool disposing) and disposal state tracking
- Changed OnError and OnChecksumMismatch from fields to properties delegating to FileProvider
- Removed UseAutoSaveInterval property; simplified AutoSave interval logic
- Changed AutoSaveInterval default value from 3 to 0
- Updated lock management: moved lock handling into individual methods instead of passing to parser
- Added ThrowIfDisposed() validation to prevent operations on disposed objects

### 1.5.6.4 — February 3, 2026

#### List of changes

- Added 2 new Actions
- Changed license

### 1.5.6.3 — February 1, 2026

#### List of changes

- Added a lock for NeoIniReader.Dispose
- Added a warning header to the INI file.

### 1.5.6.2 — February 1, 2026

#### List of changes

- Removed unnecessary async methods:
  - SectionExistsAsync
  - KeyExistsAsync
  - GetAllSectionsAsync
  - GetAllKeysAsync
  - GetSectionAsync
  - FindKeyInAllSectionsAsync
  - SearchAsync
  - ReloadFromFileAsync
  - DeleteFileAsync
  - DeleteFileWithDataAsync
  - GetEncryptionPasswordAsync

### 1.5.6.1 — February 1, 2026

#### List of changes

- Minor fixes

### 1.5.6 — February 1, 2026

#### List of changes

- Actions have been added for all events.
- The `GetEncryptionPasswordAsync` method has been added.

### 1.5.5 — February 1, 2026

#### List of changes

- Added more API methods:
  - GetSection
  - GetSectionAsync
  - FindKeyInAllSections
  - FindKeyInAllSectionsAsync
  - ClearSection
  - ClearSectionAsync
  - RenameKey
  - RenameKeyAsync
  - RenameSection
  - RenameSectionAsync
  - Search
  - SearchAsync
- .NET 6.0 support
- Added icon

### 1.5.4.4 — February 1, 2026

#### List of changes

- Fix: Added FileProvider = new(...) to NeoIniReader(string path, string encryptionPassword)

### 1.5.4.3 — February 1, 2026

#### List of changes

- Added fallback to the backup file in case of a read error

### 1.5.4.2 — February 1, 2026

#### List of changes

- Added IDisposable and persistence on Dispose

### 1.5.4.1 — February 1, 2026

#### List of changes

- Renaming NeoIni to NeoIniReader

### 1.5.4 — February 1, 2026

#### List of changes

- Subclassing the NeoIni class

### 1.5.3 — January 31, 2026

#### List of changes

- Added new API methods
- Removed junk code and added XML documentation

### 1.5.2 — January 31, 2026

#### List of changes

- Added asynchronous methods and EncryptionKey caching

### 1.5.1 — January 30, 2026

#### List of changes

- Garbage code removed

### 1.5 — January 30, 2026

#### List of changes

- Completely reworked the class.
- Added thread safety and stability; improved the "black box" philosophy.

---

> **Pre-1.5** — ancient history. This is essentially a different product that shares only the repository and the original idea. The architecture, API surface, file format, and quality standards are incomparable to anything above. Listed here for completeness only.

### 1.4 — February 17, 2025

#### List of changes

- Rework

### 1.3.1 — May 12, 2024

#### List of changes

- Stability improvements

### 1.3 — April 22, 2024

#### List of changes

- Stability improvements

### 1.2 — April 20, 2024

#### List of changes

- Added the ability to read data with a default value, it will be returned in case of error

### 1.1 — April 20, 2024

#### List of changes

- Added the ability to check the existence of a section
- Added the ability to check the existence of a key in a specific section

### 1.0 — April 19, 2024

#### List of changes

- INIReader has been released
