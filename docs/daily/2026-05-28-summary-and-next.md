# 2026-05-28 Summary & 2026-05-29 Plan

## Today (2026-05-28)

### History & import/export

- History JSON import: SQLite transaction batch insert; background import to avoid settings freeze.
- Import UX: tri-state dialog (追加 / 清空后导入 / 取消); non-modal `AppDialog` with `Topmost` and draggable header.
- `HistoryStore.TryAdd` deduplicates consecutive identical source+target rows.

### Hover translate

- `AppWindowHoverGuard` + `UiAutomationAppScope`: do not read or translate text from 炫译 own windows.
- Hover dedup (`_lastHoverTranslated`); skip cache hits for history writes.
- **译文窗关闭**且未开启「悬停时在译文窗显示」时，不执行悬停翻译、不写历史（避免仅开历史窗时后台持续记条）。
- Restored hover on other apps when 译文窗 is open (removed global pause on panel visible).

### Copy & panels

- Bilingual copy (`TranslationCopyText`); history「复制」未选中时复制全部。
- Floating panel hit-test (`#01000000` window background).

### Build

- `create-desktop-shortcut.ps1`: kill process, publish win-x64, refresh 炫译.lnk, `build-stamp.txt`.

### Also in tree (ongoing)

- Screenshot OCR / region selector, settings nav refactor, feature guide, tray slimming, general theme/font settings.

## Tomorrow (2026-05-29)

1. **回归测试**（本机 publish）：导入大 JSON、三按钮取消、译文窗关+历史窗开时悬停不写库、悬停+译文窗开时正常。
2. **悬停/历史**：确认 `AppWindowHoverGuard` 多显示器与高 DPI；必要时加「静默悬停仍记历史」可选开关（若产品需要）。
3. **截屏翻译**：端到端验证 `Ctrl+Shift+S`、OCR 失败提示、与设置/托盘入口一致。
4. **CHANGELOG**：合并重复条目，补全 Unreleased 今日改动。
5. **测试**：`HistoryStore` 导入/去重单元测试；可选 UI 自动化冒烟。
6. **未排期**：固定区域热键、OCR 悬停回退、`main` 发布标签。
