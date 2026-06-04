# 炫译 1.0.2 发行说明

**日期：** 2026-05-21  
**平台：** Windows 10 19041+（x64）  
**运行时：** [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 概述

补丁版本：修复开启「选中后自动翻译」或热键取词时，剪贴板被占用导致 **无法复制/粘贴文件** 的问题。

## 修复

- 剪贴板已有 **文件、图片** 等内容时，不再清空并模拟 Ctrl+C
- 取词失败恢复剪贴板时 **立即释放所有权**，避免长期占用系统剪贴板

## 安装

1. 下载下方 `XuanYi-1.0.2-win-x64.zip`
2. 安装 .NET 8 Desktop Runtime（若尚未安装）
3. 解压后运行 `HoverTranslate.exe`

**升级：** 托盘 → **退出炫译**，再解压覆盖旧文件。

## 回归

见 [docs/REGRESSION-translate-paths.md](https://github.com/an928159783-blip/xuanyi/blob/develop/docs/REGRESSION-translate-paths.md)。
