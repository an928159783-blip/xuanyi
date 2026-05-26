# 分支策略

| 分支 | 用途 |
|------|------|
| `main` | 稳定可发布；仅合并已自测的 `develop` |
| `develop` | 日常开发默认分支 |

## 工作流

1. 从 `develop` 拉 `feature/简短描述`（可选，单人也可直接在 `develop` 提交）。
2. 本地：`dotnet test`、`scripts/run-smoke-tests.ps1`。
3. 合并到 `develop` → 发布验证后再合并 `main`。

## 版本标签

发布时在 `main` 打 tag：`v0.1.0`，并更新 `CHANGELOG.md`。
