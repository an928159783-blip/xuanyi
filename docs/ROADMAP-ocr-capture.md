# 取词修复 + OCR 截屏 + 多语言路线图

> 依据用户反馈（2026-05-26）：可选中复制仍提示「未读到文字」；需框选截屏 + 固定区域截屏；中英互译并可扩展语言。

## 一、问题 1（已排期修复）：能选中却不翻译

**现象：** 译文窗正常弹出，提示「未读到文字」（如 Word/PDF/浏览器内已蓝选 `Optical Character Recognition`）。

**根因（代码层）：**

1. 热键后 `TryCopyClearAndCtrlC` 会**先清空剪贴板**再模拟 Ctrl+C；选区若已失效则读空。
2. 「复制后翻译」触发后**再次取词**，未直接使用已复制的剪贴板文本。
3. 部分应用不暴露 UIA `TextPattern` 选区，只能靠剪贴板。

**P0 修复（v0.1.1）：**

| 项 | 说明 |
|----|------|
| 剪贴板优先 | 热键后、模拟复制**之前**先读剪贴板 |
| 直通复制 | `TranslateOnCopy` 将已识别文本直接送入翻译，不二次清空 |
| Coordinator 兜底 | `preCaptured` 为空时再读剪贴板 |
| 错误文案 | 使用配置中的真实热键字符串 |

**验收：** 选中 → 热键；或选中 → Ctrl+C（复制后翻译）→ 稳定译出。

---

## 二、问题 2：无法选中时的悬停翻译

**能力名：** OCR 悬停（UIA 失败回退）

| 模块 | 技术 |
|------|------|
| 截屏 | 光标中心矩形（可配置 320×120 等），DPI 感知 |
| OCR | `Windows.Media.Ocr`（Win10+，离线） |
| 触发 | 现有悬停防抖 +「启用 OCR 悬停」开关（默认关） |
| 管线 | UIA 无结果 → OCR → `TranslateOrchestrator` |

**工时：** 约 4–5 天（依赖 P0 稳定）

---

## 三、问题 3：截屏翻译（两种都要）

| 模式 | 热键（建议） | 说明 |
|------|--------------|------|
| **框选一次** | `Ctrl+Shift+S` | 全屏遮罩拖拽矩形 → OCR → 翻译 |
| **固定区域** | `Ctrl+Shift+D` | 配置中保存 1–3 个矩形，一键截屏翻译 |

**共用：** `ScreenCaptureService`、`OcrService`、`ScreenshotTranslateCoordinator`

**隐私：** 仅用户按键触发；Bitmap 不持久化（除非调试开关）

**工时：** 约 5–7 天（与 OCR 模块共用）

---

## 四、中英互译与扩展语言

**v0.1.1（随 P0）：**

- `config.translationDirection`：`auto` | `en-to-zh` | `zh-to-en`
- `auto`：按 `TextHeuristics` 判断主要语种
- 微软 / 谷歌 API 动态 `from`/`to`；LLM 系统提示随方向变化

**v0.2+（扩展）：**

```json
{
  "translationDirection": "auto",
  "extraLocalePairs": [
    { "id": "ja-zh", "from": "ja", "to": "zh-Hans", "label": "日→中" }
  ]
}
```

设置页：下拉「自动 / 英→中 / 中→英 / 自定义…」

---

## 五、推荐排期

```text
v0.1.1  P0 剪贴板优先 + 复制直通 + 中英互译     ← 当前迭代
v0.2.0  OCR 模块 + 框选截屏 + 固定区域
v0.2.1  OCR 悬停回退 + 设置说明 + 兼容性文档
```

---

## 六、你侧临时用法（P0 发布前）

1. 选中英文 → **Ctrl+C** → 再按翻译热键（剪贴板兜底）。
2. 确认设置里已保存 API Key。
3. 从 `publish\HoverTranslate.exe` 启动最新包。
