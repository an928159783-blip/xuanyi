# Changelog

All notable changes to 炫译 (HoverTranslate) are documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- History JSON import (merge / replace / cancel); batch SQLite import on background thread.
- `AppWindowHoverGuard` and `UiAutomationAppScope` to ignore hover capture on 炫译 windows.
- Tri-state Frost import dialog; non-modal `AppDialog` (draggable, topmost).
- Screenshot region translate: drag to select, Windows OCR, then translate (`Ctrl+Shift+S` / tray menu).
- Frost-style `AppDialog` replaces system MessageBox across settings, history, tray flows.
- Optional「保存后自动关闭设置窗口」when user prefers close-on-save.
- Unified auto zh↔en for selection, hotkey, copy, and hover; optional「选中后自动翻译」.
- Clipboard-first capture and copy-to-translate passthrough; bidirectional zh/en (`translationDirection`).
- Settings API data-residency notice; translation panel machine-translation disclaimer (P2).
- Privacy policy, terms, compliance/distribution docs; settings & tray links for legal docs and update check (P3).
- Floating panel close button on translation and history windows.
- P1: coverlet coverage script, MockHttp integration tests, gitleaks config, diagnostic ZIP export, history JSON export.
- Tray menu and settings: export diagnostics; rolling `startup.log`.

### Changed
- Settings save no longer closes the window by default (`CloseSettingsAfterSave` defaults to false).
- Hover translate runs only when translation panel is open or「悬停时在译文窗显示」is enabled; hover skips history when panel stays closed.
- History `TryAdd` dedupes identical latest row; copy-all when no list selection.
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
