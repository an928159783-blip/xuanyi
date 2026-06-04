# Changelog

All notable changes to 炫译 (HoverTranslate) are documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [1.0.2] - 2026-05-21

### Fixed
- Clipboard: skip simulated copy when clipboard holds files/images; restore with `copy:false` so 炫译 no longer blocks Explorer file copy/paste when selection translate or hotkey capture runs.

## [1.0.1] - 2026-05-21

### Fixed
- App update check: correct GitHub Releases URL and online latest-release detection (`AppBranding`).

### Changed
- Publish bundle excludes debug symbols (`.pdb`) and internal compliance memo from install package.
- README and release notes aligned for public GitHub distribution.

## [1.0.0] - 2026-06-02

### Added
- Translation display policy: close panel suppresses passive copy/hover/selection (no API, no overlay).
- Panel lifecycle: history/translation windows hide on close instead of destroying singleton.
- `docs/REGRESSION-translate-paths.md`, `docs/RELEASE-NOTES-1.0.0.md`, `.cursor/rules/translate-display-paths.mdc`.
- Selection fallback after pointer release; ClipboardGuard / HoverGuard.
- Settings: wrapped checkbox labels; improved error dialog scrolling.
- History JSON import (merge / replace); Frost import dialog; screenshot region OCR translate.
- Multi-provider Auto failover; diagnostic ZIP export; feature guide and startup notice.

### Changed
- Default hover delay 600ms; suppress-after-close semantics documented in settings.
- Settings save default: keep window open; hover/selection capture improvements.
- History panel close/reopen no longer throws on second Show().

### Fixed
- Taskbar close no longer breaks history/translation window reopen.
- Translation coordinator presentation matrix (`ResolvePresentation`).
- Interface list scroll; opacity migration for history panel.

## [0.1.0] - 2026-05-26

### Added
- Windows tray app: global hotkey translation, hover translation, clipboard translate.
- Multi-provider support: OpenAI-compatible, Microsoft Translator, Google Cloud Translation, auto failover.
- Settings UI with Frost theme; local config at `%USERPROFILE%\.hover-translate\config.json`.
- Hover protection: debounce, cache, rate limit, 429 backoff.
- Optional SQLite history; sanitize and glossary.
- Core unit tests and smoke test script.
- Portable publish and desktop launcher scripts.

[Unreleased]: https://github.com/an928159783-blip/xuanyi/compare/v1.0.2...develop
[1.0.2]: https://github.com/an928159783-blip/xuanyi/releases/tag/v1.0.2
[1.0.1]: https://github.com/an928159783-blip/xuanyi/releases/tag/v1.0.1
[1.0.0]: https://github.com/an928159783-blip/xuanyi/releases/tag/v1.0.0
[0.1.0]: https://github.com/an928159783-blip/xuanyi/releases/tag/v0.1.0
