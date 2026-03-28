[![EN](https://img.shields.io/badge/CONTRIBUTING-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CONTRIBUTING.md)
[![RU](https://img.shields.io/badge/CONTRIBUTING-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CONTRIBUTING-RU.md)

## Contributing to NeoIni

NeoIni welcomes bug reports, feature requests, and pull requests. This guide covers the process for each.

---

### Bug reports

Open a [GitHub Issue](https://github.com/Lonewolf239/NeoIni/issues) with:

- **NeoIni version** (current: `3.3`) and **.NET version**.
- **Minimal reproduction** — code snippet or repo that triggers the bug.
- **Expected vs. actual behavior.**
- **Stack trace** (if applicable).

Check existing issues before opening a duplicate.

> **Security vulnerabilities:** Do not report in public issues. Follow the [Security Policy](./SECURITY.md).

---

### Feature requests

Open an issue with the **Feature Request** label. Include:

- **Use case** — what problem does the feature solve?
- **Proposed API** — how would the feature look from the caller's perspective?
- **Alternatives considered** — other approaches you evaluated.

Features that align with the [Roadmap](./ROADMAP.md) are prioritized.

---

### Development workflow

1. **Fork & clone** the repository.
2. **Create a branch** from `main`:
   - Bug fix: `fix/short-description`
   - Feature: `feature/short-description`
   - Docs: `docs/short-description`
3. **Write code** following the existing conventions:
   - All public API changes require XML doc comments.
   - Thread safety: acquire the internal lock for any shared-state access.
   - Async methods must accept and forward `CancellationToken`.
   - No additional dependencies without prior discussion.
4. **Test** your changes — include unit tests for new functionality and regression tests for bug fixes.
5. **Open a pull request** against `main`.

---

### Pull request requirements

| Requirement | Details |
|-------------|---------|
| Branch | Based on latest `main` |
| Scope | One logical change per PR |
| Tests | All existing tests pass; new tests cover the change |
| Docs | Update relevant docs if the public API changes |
| Commit messages | Imperative mood, concise (`Fix checksum race on concurrent reload`) |

PRs are reviewed by the maintainer. Address feedback promptly — stale PRs may be closed after 30 days.

---

### License

By contributing, you agree that your contributions are licensed under the [MIT License](./LICENSE).
