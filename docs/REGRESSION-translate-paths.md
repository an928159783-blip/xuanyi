# 翻译路径回归清单（1.0）

> 每次改 `TranslationCoordinator` / `HoverTranslateService` / 浮窗关闭逻辑后跑一遍。  
> 构建版本见 `publish/build-stamp.txt` 或设置 → 关于。

## 前置

1. 托盘 **退出炫译**
2. 从桌面 **炫译** 快捷方式启动（或 `publish\HoverTranslate.exe`）
3. 设置中已配置可用 API Key

## 六项（必测）

| # | 操作 | 预期 |
|---|------|------|
| 1 | 未关 suppress：复制英文 | 弹出大译文窗或浮层（看设置） |
| 2 | 拖选英文松手 | 约 0.5s 内翻译 |
| 3 | 未 suppress：悬停英文 | 浮层或大窗（看「悬停时在译文窗显示」） |
| 4 | 勾选「手动关闭后…」→ 关译文窗 → 复制/悬停/选中 | **完全无反应**（不 API、不浮层） |
| 5 | suppress 期间按翻译热键 | 仍可翻译并开大窗 |
| 6 | 托盘「打开译文窗」→ 再复制 | 被动自动翻译恢复 |

## 附加（发布前）

| # | 操作 | 预期 |
|---|------|------|
| 7 | 历史窗：打开 → 关 → 托盘再开 | 不报错 |
| 8 | 框选截屏热键 | OCR 后翻译 |
| 9 | 设置保存 | 热键/浮窗外观即时生效 |
| 10 | 历史 JSON 导入/导出 | 成功 |

## 自动化

```powershell
cd C:\Users\User\Projects\hover-translate
dotnet test HoverTranslate.Core.Tests -c Release
.\scripts\scan-secrets.ps1
.\scripts\run-smoke-tests.ps1
```
