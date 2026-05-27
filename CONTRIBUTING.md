# 贡献与 PR 自检

## 分支

日常开发在 `develop`，稳定发布合并到 `main`。见 [docs/branch-strategy.md](docs/branch-strategy.md)。

## 提交前检查

```powershell
dotnet test HoverTranslate.Core.Tests -c Release
.\scripts\run-smoke-tests.ps1
.\scripts\scan-secrets.ps1
```

UI 改动后：

```powershell
dotnet publish HoverTranslate.App\HoverTranslate.App.csproj -c Release -r win-x64 --self-contained false
```

## PR 自检清单

- [ ] `CHANGELOG.md` 已更新（Unreleased 或新版本）
- [ ] 无 `config.json`、`.env`、`*.db` 进入提交
- [ ] 新功能有 Core 单测或说明为何无法测
- [ ] 已本地跑冒烟测试
- [ ] 未提交 API Key 或真实密钥

## 覆盖率（可选）

```powershell
.\scripts\run-coverage.ps1
```

目标：`HoverTranslate.Core` 行覆盖率 ≥ 70%。
