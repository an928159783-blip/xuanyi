# 炫译 HoverTranslate 1.0 发布方案

> 更新：2026-06-02。基于 `develop` @ 1.0.0 准备发布。

## 一、1.0 产品定义

**炫译 1.0** = 可日常使用的 Windows 托盘翻译工具，核心能力稳定、设置可配置、安装/升级路径清晰。

| 能力 | 1.0 必须 | 说明 |
|------|----------|------|
| 全局热键翻译 | ✅ | Ctrl+Shift+T（可改），剪贴板优先 |
| 复制后自动翻译 | ✅ | 可开关 |
| 选中即译 | ✅ | UIA + 松手后模拟复制兜底 |
| 悬停翻译 | ✅ | 段落→行→词；译文窗或轻量浮层 |
| 多接口 / Auto 轮询 | ✅ | DeepSeek、百炼、Azure、Google 等 |
| 译文窗 / 历史窗 | ✅ | 主题、字号、不透明度 |
| 框选截屏 OCR 翻译 | ✅ | Ctrl+Shift+S + 托盘入口 |
| 本地历史 SQLite | ✅ | 导入/导出/清空 |
| 设置 UI（Frost） | ✅ | 六页导航，保存即生效 |
| 隐私与合规文档 | ✅ | 设置内链接 |
| 诊断包导出 | ✅ | 关于页 |
| 桌面快捷方式安装 | ✅ | `create-desktop-shortcut.ps1` |

| 能力 | 1.0 不做 / 1.1+ | 说明 |
|------|-----------------|------|
| OCR 悬停回退 | 1.1 | 见 ROADMAP-ocr-capture.md |
| 固定区域截屏热键 | 1.1 | Ctrl+Shift+D |
| MSIX / 商店安装包 | 1.2 | 见 store-msix-plan.md |
| 多语言对（日/韩等） | 1.2+ | 架构已预留 direction |

---

## 二、设置页功能清单（必须全部有效）

### 翻译接口
- [x] 翻译通道 / Auto 勾选
- [x] 添加 / 编辑 / 删除接口
- [x] 获取 API Key 链接
- [x] 接口列表框内滚轮（隐藏滚动条 + 边界传递）

### 浮窗
- [x] 译文/历史主题、不透明度、预设、实时预览
- [x] 全局浮窗字号/字体（保存 + 拖动预览）
- [x] 附加项、显示策略勾选
- [x] 历史导出/导入/清空

### 热键与悬停
- [x] 翻译热键录制/恢复默认
- [x] 截屏热键录制/恢复默认
- [x] 选中即译 / 复制后翻译 / 悬停 / 延迟
- [x] 翻译方向
- [x] 立即框选截屏

### 通用 / 隐私 / 关于
- [x] 保存后关闭、启动提示
- [x] 自动脱敏
- [x] 配置目录、政策链接、功能介绍、检查更新、诊断包

**回归方式：** 逐项改 → 保存 → 验证托盘/热键/浮窗行为；见 `scripts/smoke-test.ps1`（可扩展）。

---

## 三、近期已修复（面向 1.0）

| 问题 | 处理 |
|------|------|
| 悬停应先段落再行/词 | `UiAutomationTextCapture` 段落→行→词 |
| 关译文窗后被动翻译 | 勾选「手动关闭后…」→ 复制/选中/悬停完全静默；热键仍可翻译；托盘打开译文窗恢复 |
| 设置页内误悬停翻译 | `SettingsWindowHost.IsOpen` 时禁用 tick |
| 选中即译过钝 | 松手 120ms 后走热键同款 `GetTextForTranslation` |
| 接口列表滚动 | ListBox 内滚 + 外层隐藏滚动条 |

---

## 四、1.0 发布前待办（按优先级）

### P0 — 阻塞发布
1. **全量回归脚本**：热键、复制、选中、悬停、截屏、保存设置、历史导入 JSON/TXT
2. **版本号统一**：Assembly、`AboutVersionText`、`build-stamp.txt` → `1.0.0`
3. **默认配置审查**：`config.json` 默认值符合新用户预期（悬停开、选中可选、关闭译文窗不自动弹）
4. **安装包**：确认 `dotnet publish -r win-x64` + 桌面快捷方式 + 可选 ZIP 分发

### P1 — 强烈建议
5. **首次运行向导**：无 API Key 时打开设置并聚焦接口页（已有部分逻辑）
6. **更新 CHANGELOG** `[1.0.0]` 条目
7. **Git tag** `v1.0.0` + Release Notes（中文）
8. **悬停浮层开关**：设置中明确「悬停时在译文窗显示 / 否则轻量浮层 / 关闭浮层仅写历史（可选）」

### P2 — 1.0 后可跟
9. OCR 悬停回退（Electron/PDF）
10. 固定区域截屏
11. 自动更新渠道（检查更新已有，需服务端 manifest）

---

## 五、1.0 安装包制作步骤

```powershell
# 1. 退出托盘中的炫译
# 2. 发布
cd C:\Users\User\Projects\hover-translate
dotnet test HoverTranslate.Core.Tests -c Release
powershell -ExecutionPolicy Bypass -File scripts\create-desktop-shortcut.ps1

# 3. 产物路径
# HoverTranslate.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\

# 4. 可选：打 ZIP
$pub = "HoverTranslate.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
Compress-Archive -Path "$pub\*" -DestinationPath "dist\XuanYi-1.0.0-win-x64.zip" -Force
```

**用户安装：** 解压 publish 目录 → 运行 `HoverTranslate.exe` → 或运行 `scripts\create-desktop-shortcut.ps1` 创建「炫译」快捷方式。

**系统要求：** Windows 10 19041+，.NET 8 Desktop Runtime（框架依赖 publish 时用户需已安装运行时）。

---

## 六、推荐默认配置（1.0）

| 项 | 建议默认 |
|----|----------|
| EnableHover | true |
| TranslateOnSelection | false（用户自行开启，避免误触） |
| TranslateOnCopy | false |
| ShowPanelOnTranslate | true |
| ShowPanelOnHover | false（用轻量浮层，少挡屏幕） |
| SuppressPanelAfterUserClose | true |
| HoverDelayMs | 600 |
| EnableHistory | true |

---

## 七、代码结构（维护入口）

| 模块 | 文件 |
|------|------|
| 悬停/选中 tick | `HoverTranslateService.cs` |
| 悬停取词 | `UiAutomationTextCapture.cs` |
| 选中取词 | `UiAutomationSelectionCapture.cs` + `SelectionCaptureService.cs` |
| 翻译编排 | `TranslationCoordinator.cs` |
| 设置 UI | `SettingsWindow.xaml(.cs)` |
| 发布 | `scripts/create-desktop-shortcut.ps1` |
