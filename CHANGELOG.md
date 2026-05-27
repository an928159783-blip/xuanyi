# Changelog

All notable changes to 炫译 (HoverTranslate) are documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Clipboard-first capture and copy-to-translate passthrough; bidirectional zh/en (`translationDirection`).
- Settings API data-residency notice; translation panel machine-translation disclaimer (P2).
- Privacy policy, terms, compliance/distribution docs; settings & tray links for legal docs and update check (P3).
- Floating panel close button on translation and history windows.
- P1: coverlet coverage script, MockHttp integration tests, gitleaks config, diagnostic ZIP export, history JSON export.
- Tray menu and settings: export diagnostics; rolling `startup.log`.

### Changed
- `MicrosoftTranslator` accepts injectable `HttpClient` for tests.
- `CONTRIBUTING.md` with PR checklist.

## [0.1.0] - 2026-05-26

### Added
- Windows tray app: global hotkey translation, hover translation, clipboard translate.
- Multi-provider support: OpenAI-compatible, Microsoft Translator, Google Cloud Translation, auto failover.
- Settings UI with Frost theme; local config at `%USERPROFILE%\.hover-translate\config.json`.
- Hover protection: debounce, cache, rate limit, 429 backoff.
- Optional SQLite history; sanitize and glossary.
- Core unit tests (18) and smoke test script.
- Portable publish and desktop launcher scripts.

[Unreleased]: https://github.com/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/releases/tag/v0.1.0
