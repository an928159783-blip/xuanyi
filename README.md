# 炫译 (XuanYi / HoverTranslate)

Windows 托盘热键与悬停翻译工具。支持 **OpenAI 兼容**、**微软 Azure Translator**、**谷歌 Cloud Translation**，以及 **Auto 多接口轮询**。

密钥仅存本机 `%USERPROFILE%\.hover-translate\`，详见 [SECURITY.md](SECURITY.md)。

## 开源

本项目以 [MIT](LICENSE) 发布，Copyright © 2026 **沧溟散人**。欢迎 Issue 与 PR。

- **下载安装包：** [Releases](https://github.com/an928159783-blip/xuanyi/releases)
- **变更记录：** [CHANGELOG.md](CHANGELOG.md)
- **分支策略：** [docs/branch-strategy.md](docs/branch-strategy.md)

## 功能

- 全局热键（可自定义录制）：翻译选中英文
- 悬停翻译、复制后自动翻译
- 深色设置界面：添加/编辑多个翻译接口
- 本地 SQLite 历史（可选）、脱敏、悬浮窗自动关闭
- 蓝白「炫译」应用图标

## 环境要求

- Windows 10 19041+ / Windows 11（x64）
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（运行已 publish 的 exe 时需要）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（仅开发/编译）

## 安装 / 升级（1.0）

1. 解压 `dist\XuanYi-1.0.0-win-x64.zip` 或 `publish\` 目录
2. 运行 `HoverTranslate.exe`；或 `.\scripts\create-desktop-shortcut.ps1` 创建桌面「炫译」
3. **升级前** 托盘 → **退出炫译**（运行中进程不会加载新 exe）

发行说明见 [docs/RELEASE-NOTES-1.0.0.md](docs/RELEASE-NOTES-1.0.0.md)；回归见 [docs/REGRESSION-translate-paths.md](docs/REGRESSION-translate-paths.md)。

## 快速开始

```powershell
cd $env:USERPROFILE\Projects\hover-translate
dotnet build -c Release
dotnet run --project HoverTranslate.App -c Release
```

首次运行托盘提示 → 打开 **设置 → 翻译接口**，添加 API 并保存。

## 配置

`%USERPROFILE%\.hover-translate\config.json` — 参考 [config.example.json](config.example.json)。

| `apiProfiles[].kind` | 说明 |
|----------------------|------|
| `openai` | DeepSeek、百炼、自建 OpenAI 兼容 Chat API |
| `microsoft` | Azure Translator（需 Key + 区域） |
| `google` | Google Cloud Translation API v2 |

`provider`: `auto` 或某个 profile 的 `id`。

## 便携包 / 续开发

```powershell
.\scripts\package-portable.ps1
```

输出 `dist\XuanYi-HoverTranslate-portable-*.zip`，内含 `publish\` 可运行 exe 与 `source\` 源码。详见 [README-MIGRATION.md](README-MIGRATION.md)。
