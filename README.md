# 炫译 (XuanYi / HoverTranslate)

Windows 托盘热键与悬停翻译工具。支持 **OpenAI 兼容**、**微软 Azure Translator**、**谷歌 Cloud Translation**，以及 **Auto 多接口轮询**。

密钥仅存本机 `%USERPROFILE%\.hover-translate\`，详见 [SECURITY.md](SECURITY.md)。

## 许可证

[MIT](LICENSE) · 变更见 [CHANGELOG.md](CHANGELOG.md) · 分支策略见 [docs/branch-strategy.md](docs/branch-strategy.md)。

## 功能

- 全局热键（可自定义录制）：翻译选中英文
- 悬停翻译、复制后自动翻译
- 深色设置界面：添加/编辑多个翻译接口
- 本地 SQLite 历史（可选）、脱敏、悬浮窗自动关闭
- 蓝白「炫译」应用图标

## 环境要求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（开发/编译）

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
