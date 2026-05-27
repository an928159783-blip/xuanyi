# Microsoft Store / MSIX 上架方案

## 决策摘要（2026-05-26）

| 路径 | 建议阶段 | 说明 |
|------|----------|------|
| **便携 zip + GitHub Releases** | 当前 MVP | 成本低，适合技术用户；无商店审核 |
| **MSIX + Microsoft Store** | 有稳定用户与主体资质后 | 需 Partner Center、隐私 URL、审核周期 |

**当前结论：** 先 **zip 便携分发**，并行准备 Store 材料；待 P3-01 隐私政策可公网访问后再提交商店。

## MSIX 技术路径（WPF）

1. 在解决方案增加 `Windows Application Packaging Project` 或 `dotnet publish` + `makeappx` 生成 MSIX。
2. 设置 `Package.appxmanifest`：显示名称「炫译」、发布者 CN、能力（internetClient、runFullTrust 桌面桥）。
3. 桌面桥允许 WPF WinExe；测试悬停钩子、托盘、全局热键在打包后是否正常。
4. 签名：测试用自签；商店用 Partner Center 管理的证书。

## Partner Center 清单

- [ ] 注册 Microsoft 开发者账号（公司或个人）
- [ ] 应用名称、描述（中/英）、分类「生产力」
- [ ] 隐私政策 **HTTPS URL**（不可仅本地 md）
- [ ] 至少 4 张 1366×768 截图
- [ ] 年龄分级问卷
- [ ] 安装包：MSIX 或 MSIXBundle

## 审核风险点

- 声明第三方 API 与用户自备密钥
- 数据出境说明与隐私政策一致
- 不使用误导性「官方翻译」表述

## 若放弃 Store

在 `进度看板.md` 记录原因（如：钩子权限、审核周期、主体资质），专注 GitHub Releases + 可选官网。
