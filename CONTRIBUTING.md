# Contributing to NeoIni

Thank you for your interest in **NeoIni**! We welcome contributions of all forms: from bug reports to code contributions and documentation improvements.

This document outlines the process for contributing to the project.

## Bug Reports

Before creating an Issue, please:
1.  **Check the version.** Ensure you are using the current version of the library (1.6.0.1). We do not support older versions.
2.  **Use search.** It is possible that this problem has already been discussed or resolved.

If you have found a new bug, create an Issue and include:
-   The library version and .NET version.
-   A minimal code snippet that reproduces the error.
-   The expected and actual behavior.
-   If the error is related to file corruption, please attach (if possible) an anonymized example file.

> **Important:** If you discover a security vulnerability (encryption, data leak), please follow our [Security Policy](SECURITY.md) and do not publish details in public Issues.

## Feature Requests

We strive to adhere to the "Black Box" philosophy: the library should be simple on the outside and powerful on the inside. If you want to suggest a new feature:
1.  Create an Issue with the label `enhancement`.
2.  Describe what problem it solves.
3.  Explain why this should be part of the core library rather than an external extension.

## Development and Pull Requests

We use the standard GitHub Flow process:

1.  **Fork** the repository.
2.  Create a branch for your task:
    -   `fix/issue-number-description` — for fixes.
    -   `feature/feature-name` — for new features.
3.  **Code Requirements:**
    -   Platform: **.NET 6.0**.
    -   Follow standard C# code style (PascalCase for methods and global variables, camelCase for local variables).
    -   **Thread Safety:** Any code working with config data MUST be thread-safe (use existing locking mechanisms like `ReaderWriterLockSlim`).
    -   **Asynchrony:** If an operation takes time (IO), implement an `Async` version.
4.  Ensure the project builds without errors or warnings.
5.  Submit a Pull Request to the `main` branch.

## Licensing

By submitting code to the NeoIni repository, you agree to license your contribution under the terms of the MIT License used in this project.
