namespace HoverTranslate.App.Windows;

internal static class TranslationCopyText
{
    /// <summary>与译文窗一致：原文在上，译文在下。</summary>
    public static string Combine(string? source, string? target)
    {
        source = source?.Trim() ?? "";
        target = target?.Trim() ?? "";
        if (string.IsNullOrEmpty(target) || target == "…")
            return source;
        if (string.IsNullOrEmpty(source) || string.Equals(source, target, StringComparison.Ordinal))
            return target;

        return $"{source}{Environment.NewLine}{Environment.NewLine}{target}";
    }
}
