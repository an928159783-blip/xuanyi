using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public sealed class AutoTranslator : ITranslator
{
    private readonly AppConfig _config;
    private readonly GlossaryService _glossary;

    public string ProviderName => "auto";

    public AutoTranslator(AppConfig config, GlossaryService glossary)
    {
        _config = config;
        _glossary = glossary;
    }

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken ct = default)
    {
        var candidates = ProviderResolver.GetAutoCandidates(_config);
        if (candidates.Count == 0)
            return TranslationResult.Fail(text, "Auto 模式：请先在设置里添加并填写至少一个翻译接口的 API Key。");

        string? lastError = null;
        foreach (var profile in candidates)
        {
            try
            {
                var translator = ProfileTranslatorFactory.Create(profile, _config, _glossary);
                var result = await translator.TranslateAsync(text, ct).ConfigureAwait(false);
                if (result.Success)
                    return TranslationResult.Ok(
                        result.SourceText,
                        result.TranslatedText,
                        ApiProfileDisplay.GetPrimaryLabel(profile),
                        result.Model,
                        result.PromptTokens,
                        result.CompletionTokens,
                        result.TotalTokens);
                lastError = result.ErrorMessage;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return TranslationResult.Fail(text, lastError ?? "Auto 模式：所有接口均失败。");
    }
}
