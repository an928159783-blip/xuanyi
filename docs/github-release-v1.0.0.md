# 炫译 1.0.0 发行说明

**日期：** 2026-06-02  
**平台：** Windows 10 19041+（x64）  
**运行时：** [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 概述

炫译 1.0 是可日常使用的 Windows 托盘翻译工具：热键、复制、选中、悬停、框选截屏 OCR，多接口 Auto 轮询，本地历史与 Frost 设置界面。

## 主要能力

- 全局热键翻译（默认 Ctrl+Shift+T）
- 复制后自动翻译、选中即译、悬停翻译（均可单独开关）
- 译文窗 / 历史窗：主题、字号、不透明度
- 关译文窗后暂停被动自动翻译（热键仍可用；托盘可恢复）
- 多翻译接口与 Auto failover
- 本地 SQLite 历史（导入/导出/清空）
- 诊断包导出、检查更新

## 安装

1. 在 [Releases](https://github.com/an928159783-blip/xuanyi/releases) 下载 `XuanYi-1.0.0-win-x64.zip`
2. 安装 .NET 8 Desktop Runtime（若尚未安装）
3. 解压后运行 `HoverTranslate.exe`

**升级：** 必须先托盘「退出炫译」，再覆盖 `HoverTranslate.exe`（运行中的进程不会加载新文件）。

> **提示：** 建议升级至 [v1.0.1](https://github.com/an928159783-blip/xuanyi/releases/tag/v1.0.1) 以获得正确的「检查更新」链接。

## 已知限制（1.1 计划）

- 无 MSIX / 商店安装包
- 无代码签名（SmartScreen 可能提示）
- Electron/PDF 等场景建议热键或截屏

## 回归

见 [docs/REGRESSION-translate-paths.md](https://github.com/an928159783-blip/xuanyi/blob/develop/docs/REGRESSION-translate-paths.md)。
