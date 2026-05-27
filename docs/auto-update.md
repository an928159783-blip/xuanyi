# 自动更新方案

## 目标

用户能获知新版本并安全升级，避免长期运行过期的 `publish\HoverTranslate.exe`。

## 方案对比

| 方案 | 优点 | 缺点 | 炫译阶段 |
|------|------|------|----------|
| **手动 GitHub Releases** | 零依赖、透明 | 需用户自行下载替换 | ✅ 当前 |
| **托盘打开 Releases 页** | 实现极简 | 非静默更新 | ✅ 已实现（MVP） |
| **version.json + 提示** | 可检测新版本 | 需托管 JSON 与签名习惯 | 下一迭代 |
| **Velopack** | .NET 友好、差分更新 | 需签名与发布流水线 | 用户量上来后 |
| **Squirrel.Windows** | 成熟 | 维护活跃度一般 | 备选 |
| **MSIX / Store** | 商店托管更新 | 审核与打包成本高 | 见 store-msix-plan.md |

## MVP 实现（v0.1.x）

- 托盘与设置 → **检查更新**：显示当前版本；尝试请求 GitHub Releases API；失败则打开 Releases 页面。
- 配置常量：`AppBranding.ReleasesPageUrl`（发布时改为真实仓库地址）。

## 推荐后续（v0.2）

1. CI 在 tag `v*` 时发布 `HoverTranslate-win-x64.zip` 与 `version.json`：

```json
{ "version": "0.2.0", "url": "https://.../HoverTranslate-0.2.0.zip", "sha256": "..." }
```

2. 启动时可选静默检查（24h 内仅一次），有新版本时托盘气泡提示。
3. 评估 **Velopack** 做一键升级；发布包必须 **Authenticode 签名** 以降低 SmartScreen 拦截。

## 安全

- 校验发布包 SHA256；优先 HTTPS。
- 不在更新通道传输 API Key。
