using HoverTranslate.Core.Models;
using HoverTranslate.Core.Translators;

namespace HoverTranslate.Core.Services;

public sealed class TranslateOrchestrator
{
    private readonly SanitizeService _sanitize = new();
    private readonly GlossaryService _glossary = new();

    public async Task<TranslationResult> TranslateAsync(string rawText, AppConfig config, bool fromHover = false, CancellationToken ct = default)
    {
        var text = rawText.Trim();
        if (string.IsNullOrEmpty(text))
            return TranslationResult.Fail("", "没有可翻译的文本。请先选中文字或复制到剪贴板。");

        if (config.Sanitize)
        {
            var (sanitized, hadSensitive) = _sanitize.Sanitize(text);
            text = sanitized;
            if (hadSensitive && text.Replace("[REDACTED]", "").Trim().Length == 0)
                return TranslationResult.Fail(rawText, "内容主要为敏感信息，已阻止上传。");
        }

        if (!fromHover && _sanitize.LooksLikeCode(text))
            return TranslationResult.Fail(text, "检测到疑似代码。若仍需翻译，请缩短选区。");

        var translator = TranslatorFactory.Create(config, _glossary);
        return await translator.TranslateAsync(text, ct).ConfigureAwait(false);
    }
}
