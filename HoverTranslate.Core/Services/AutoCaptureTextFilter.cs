using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>自动取词时若抓到多行（如中文输入 + 侧边栏英文），只保留符合翻译方向的一行。</summary>
public static class AutoCaptureTextFilter
{
    public static string? PickTranslatable(string? text, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        if (!text.Contains('\n') && !text.Contains('\r'))
            return TranslationDirectionResolver.ShouldTranslateText(text, config) ? text : null;

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length >= 2)
            .ToList();

        if (lines.Count == 0)
            return null;

        var eligible = lines
            .Where(l => TranslationDirectionResolver.ShouldTranslateText(l, config))
            .ToList();

        if (eligible.Count == 0)
            return null;

        if (eligible.Count == 1)
            return eligible[0];

        // 多行均符合时取最长一行，避免只剩侧边栏短标签
        return eligible.OrderByDescending(l => l.Length).First();
    }
}
