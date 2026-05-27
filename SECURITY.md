# 炫译 — 数据与隐私

用户向说明见 [`docs/privacy.md`](docs/privacy.md)；使用条款见 [`docs/terms.md`](docs/terms.md)。

- **API Key**：仅保存在本机 `%USERPROFILE%\.hover-translate\config.json`。应用不会将密钥上传到炫译或任何第三方运维服务器。
- **翻译请求**：仅向您在本机配置的接口地址发送（如 Azure、Google、OpenAI 兼容端点）。请自行保管密钥并遵守各服务商条款。
- **数据出境**：若接口地址位于境外，待翻译文本将从本机直接发往该境外服务，可能适用数据出境相关规则；可在应用 **设置 → 翻译接口** 查看说明，并优先选用境内节点（如百炼、Azure 中国区）若你有合规要求。
- **翻译历史**：默认关闭；开启后写入本机 SQLite `history.db`，可随时在设置中清空。
- **脱敏**：可选在发送前掩码 token、Bearer、邮箱等敏感片段（本地规则，不上传原文到额外服务）。
