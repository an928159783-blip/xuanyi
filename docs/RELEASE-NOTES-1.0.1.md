# 炫译 1.0.1 发行说明

**日期：** 2026-05-21  
**平台：** Windows 10 19041+（x64）  
**运行时：** [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 概述

维护版本：修复应用内「检查更新」链接与在线版本检测，优化公开发布文档。

## 变更

- 设置 → 关于 → **检查更新** 指向正确 Releases 页
- 在线比对 GitHub 最新 Release 标签
- 安装包不再包含调试符号（`.pdb`）与内部合规备忘

## 安装

1. 在 [Releases](https://github.com/an928159783-blip/xuanyi/releases) 下载 `XuanYi-1.0.1-win-x64.zip`
2. 安装 .NET 8 Desktop Runtime（若尚未安装）
3. 解压后运行 `HoverTranslate.exe`

**升级：** 托盘 → **退出炫译**，再解压覆盖旧文件。

## 已知限制

- 无代码签名（SmartScreen 可能提示）
- 无 MSIX / 商店安装包

## 回归

见 [docs/REGRESSION-translate-paths.md](https://github.com/an928159783-blip/xuanyi/blob/develop/docs/REGRESSION-translate-paths.md)。
