using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>翻译方向：自动中英互译，或固定方向；预留更多语言对。</summary>
public static class TranslationDirectionResolver
{
    public const string ModeAuto = "auto";
    public const string ModeEnToZh = "en-to-zh";
    public const string ModeZhToEn = "zh-to-en";

    public static (string From, string To) ResolveApiPair(string text, AppConfig config)
    {
        var mode = NormalizeMode(config.TranslationDirection);
        return mode switch
        {
            ModeEnToZh => ("en", "zh-Hans"),
            ModeZhToEn => ("zh-Hans", "en"),
            _ => TextHeuristics.IsMostlyEnglish(text)
                ? ("en", "zh-Hans")
                : TextHeuristics.ContainsSignificantCjk(text)
                    ? ("zh-Hans", "en")
                    : ("en", "zh-Hans")
        };
    }

    public static (string Source, string Target) ResolveGooglePair(string text, AppConfig config)
    {
        var (from, to) = ResolveApiPair(text, config);
        return (from, to switch
        {
            "zh-Hans" => "zh-CN",
            _ => to
        });
    }

    public static string BuildLlmSystemPrompt(string text, AppConfig config, string glossarySection)
    {
        var mode = NormalizeMode(config.TranslationDirection);
        var targetLanguage = mode switch
        {
            ModeEnToZh => "简体中文",
            ModeZhToEn => "英文",
            _ => TextHeuristics.IsMostlyEnglish(text) ? "简体中文"
                : TextHeuristics.ContainsSignificantCjk(text) ? "英文"
                : "简体中文"
        };

        return $"""
            你是界面文案翻译器。将用户给出的文本译为{targetLanguage}。
            规则：
            1. 只输出译文，不要引号、不要解释、不要 markdown。
            2. 保留术语表中的产品名与专有名词。
            3. 若为代码块或 JSON，原样返回不翻译。

            术语表：{glossarySection}
            """;
    }

    public static string NormalizeMode(string? mode)
    {
        var m = mode?.Trim().ToLowerInvariant();
        return m switch
        {
            ModeEnToZh or ModeZhToEn => m,
            _ => ModeAuto
        };
    }

    /// <summary>选中/热键/复制：该文本是否应触发翻译（与翻译方向一致）。</summary>
    public static bool ShouldTranslateText(string text, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
            return false;

        var t = text.Trim();
        return NormalizeMode(config.TranslationDirection) switch
        {
            ModeEnToZh => TextHeuristics.IsMostlyEnglish(t),
            ModeZhToEn => TextHeuristics.ContainsSignificantCjk(t),
            _ => TextHeuristics.IsMostlyEnglish(t) || TextHeuristics.ContainsSignificantCjk(t)
        };
    }
}
