# 炫译 — 可迁移开发包说明

## 解压后快速运行（仅 Windows）

| 内容 | 平台 |
|------|------|
| `publish\` 里的 exe | **Windows 10/11 x64**，需安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0) |
| `source\` 源码 | 任意系统可浏览/改代码；**编译运行仍要 Windows** |

1. 解压 zip 后进入 **`publish\`**（不要只复制单个 exe）。
2. 双击 **`HoverTranslate.exe`** 或 **`Start-XuanYi.bat`**。
3. 桌面快捷方式请用 **`炫译.lnk`**（运行 `scripts\create-desktop-shortcut.ps1` 生成），图标来自 exe 内嵌资源。
4. 首次运行会创建 `%USERPROFILE%\.hover-translate\config.json`。
5. 托盘 → **设置** → **翻译接口**：添加 API Key 并保存。

## 从源码继续开发

```powershell
cd <解压目录>
dotnet build HoverTranslate.sln -c Release
dotnet publish HoverTranslate.App\HoverTranslate.App.csproj -c Release -r win-x64 --self-contained false
```

发布输出：`HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish\`

## 支持的翻译接口

| 类型 | `kind` | 必填 | 说明 |
|------|--------|------|------|
| OpenAI 兼容 | `openai` | API Key, Base URL, 模型 | DeepSeek、百炼、自建等 Chat Completions |
| 微软翻译 | `microsoft` | API Key, 区域 | Azure Translator Text API |
| 谷歌翻译 | `google` | API Key | Cloud Translation API v2 |

`provider`: `auto` 或某个 `apiProfiles[].id`。

## 参考 Python 版

原参考项目路径：`hover-translate (1)\hover-translate`。已吸收：模型链思路（可扩展）、悬浮窗超时、邮箱脱敏、打开配置目录、手动 Key 配置。

## 对话续开发

Agent transcript（若存在）：`agent-transcripts\8de74726-a684-4e67-ae68-8067e3f448fd.jsonl`

在新对话中说明：继续开发 **炫译** 项目，路径为本解压目录或 `C:\Users\User\Projects\hover-translate`。
