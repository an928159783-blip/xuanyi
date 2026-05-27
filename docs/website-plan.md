# 官网与下载分发决策

**日期：** 2026-05-26

## 决策

| 渠道 | 状态 | 说明 |
|------|------|------|
| GitHub Releases（zip） | **推荐首选** | 无 ICP 要求；附 SHA256；与开源仓库一致 |
| 网盘 / 社群 | 可选 | 仅作镜像，需注明版本与校验和 |
| 自建官网 + ICP | **暂缓** | 个人备案约 45–60 工作日；需持续维护 HTTPS |

**当前：** 不建独立官网；隐私政策可随仓库 `docs/privacy.md` 发布，商店上架前再同步到静态页托管（GitHub Pages / Cloudflare Pages）。

## 若未来自建站

1. 域名备案（ICP）与公安备案（如适用）
2. 静态页：产品简介、下载按钮 → 指向最新 Release zip
3. 页脚：ICP 备案号、隐私政策、用户协议链接
4. 使用 CDN + HTTPS；下载文件提供 SHA256 校验说明

## GitHub Pages 最小页（可选）

- 仓库 Settings → Pages → `docs/` 或 `gh-pages` 分支
- 将 `privacy.md` 转为 `index` 子路径或独立 HTML 供 Store 审核 URL 使用
