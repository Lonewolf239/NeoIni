[![EN](https://img.shields.io/badge/CONTRIBUTING-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CONTRIBUTING.md)
[![RU](https://img.shields.io/badge/CONTRIBUTING-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CONTRIBUTING-RU.md)

## Contributing to NeoIni

NeoIni принимает баг-репорты, предложения фич и pull request-ы. Здесь описан процесс для каждого варианта.

---

### Bug reports

Создайте [GitHub Issue](https://github.com/Lonewolf239/NeoIni/issues), указав:

- **Версию NeoIni** (текущая: `3.1`) и **версию .NET**.
- **Минимальное воспроизведение** — фрагмент кода или репозиторий, вызывающий ошибку.
- **Ожидаемое и фактическое поведение.**
- **Stack trace** (если применимо).

Проверьте существующие issues перед созданием дубликата.

> **Уязвимости безопасности:** Не сообщайте в публичных issues. Следуйте [Security Policy](./SECURITY-RU.md).

---

### Feature requests

Создайте issue с меткой **Feature Request**. Укажите:

- **Сценарий использования** — какую проблему решает фича?
- **Предлагаемый API** — как фича будет выглядеть со стороны вызывающего кода?
- **Рассмотренные альтернативы** — другие подходы, которые вы оценивали.

Фичи, соответствующие [Roadmap](./ROADMAP-RU.md), имеют приоритет.

---

### Development workflow

1. **Fork & clone** репозитория.
2. **Создайте ветку** от `main`:
   - Исправление: `fix/short-description`
   - Фича: `feature/short-description`
   - Документация: `docs/short-description`
3. **Пишите код**, следуя существующим соглашениям:
   - Все изменения публичного API требуют XML doc comments.
   - Потокобезопасность: захватывайте внутренний lock при доступе к общему состоянию.
   - Async-методы должны принимать и пробрасывать `CancellationToken`.
   - Никаких дополнительных зависимостей без предварительного обсуждения.
4. **Тестируйте** изменения — добавляйте unit-тесты для новой функциональности и regression-тесты для исправлений.
5. **Откройте pull request** в ветку `main`.

---

### Pull request requirements

| Requirement | Details |
|-------------|---------|
| Branch | На основе актуального `main` |
| Scope | Одно логическое изменение на PR |
| Tests | Все существующие тесты проходят; новые тесты покрывают изменение |
| Docs | Обновите документацию при изменении публичного API |
| Commit messages | Императивное наклонение, кратко (`Fix checksum race on concurrent reload`) |

PR-ы проверяются мейнтейнером. Отвечайте на feedback оперативно — неактивные PR могут быть закрыты через 30 дней.

---

### License

Отправляя код, вы соглашаетесь лицензировать свой вклад на условиях [MIT License](./LICENSE).
