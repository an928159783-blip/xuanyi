using System.Text.RegularExpressions;

namespace HoverTranslate.Core.Services;

public static class TextHeuristics
{
    private static readonly Regex EnglishSpanRegex = new(
        @"[A-Za-z][A-Za-z0-9\-.,'""()/+: ]{1,120}",
        RegexOptions.Compiled);

    /// <summary>是否主要为英文（整段悬停）</summary>
    public static bool IsMostlyEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var letters = text.Count(char.IsLetter);
        if (letters < 2) return false;

        var asciiLetters = text.Count(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        return (double)asciiLetters / letters >= 0.55;
    }

    /// <summary>是否含较多中文/CJK</summary>
    public static bool ContainsSignificantCjk(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var cjk = text.Count(IsCjk);
        var meaningful = text.Count(c => !char.IsWhiteSpace(c));
        if (meaningful == 0) return false;
        return (double)cjk / meaningful >= 0.2;
    }

    public static bool ShouldTranslateOnHover(string text) =>
        IsMostlyEnglish(text) && !ContainsSignificantCjk(text);

    /// <summary>悬停/指针取词：与「翻译方向」一致，自动英↔中。</summary>
    public static string? ResolveHoverTarget(string? raw, string? translationDirection = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        if (raw.Length > 800) raw = raw[..800];

        return TranslationDirectionResolver.NormalizeMode(translationDirection) switch
        {
            TranslationDirectionResolver.ModeEnToZh => ResolveEnglishTarget(raw),
            TranslationDirectionResolver.ModeZhToEn =>
                ContainsSignificantCjk(raw) ? raw : null,
            _ => ResolveAutoHoverTarget(raw)
        };
    }

    private static string? ResolveAutoHoverTarget(string raw)
    {
        if (IsMostlyEnglish(raw) && !ContainsSignificantCjk(raw))
            return raw;
        if (ContainsSignificantCjk(raw))
            return raw;
        return ExtractBestEnglishSpan(raw);
    }

    private static string? ResolveEnglishTarget(string raw)
    {
        if (ShouldTranslateOnHover(raw))
            return raw;
        if (IsEnglishSnippet(raw))
            return raw;
        return ExtractBestEnglishSpan(raw);
    }

    public static bool IsEnglishSnippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return false;
        var letters = text.Count(char.IsLetter);
        if (letters < 2) return false;
        var ascii = text.Count(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        return (double)ascii / letters >= 0.5;
    }

    private static string? ExtractBestEnglishSpan(string text)
    {
        string? best = null;
        var bestScore = 0;

        foreach (Match m in EnglishSpanRegex.Matches(text))
        {
            var span = m.Value.Trim();
            if (span.Length < 3 || !IsEnglishSnippet(span)) continue;

            var score = span.Length;
            if (span.Contains(' ')) score += 4;
            if (score > bestScore)
            {
                bestScore = score;
                best = span;
            }
        }

        return best;
    }

    private static bool IsCjk(char c) =>
        c is >= '\u4e00' and <= '\u9fff'
            or >= '\u3400' and <= '\u4dbf'
            or >= '\u3040' and <= '\u30ff';
}
