namespace HoverTranslate.App.Windows;

internal static class TranslationCopyText
{
    public static string Combine(string? source, string? target)
    {
        source = source?.Trim() ?? "";
        target = target?.Trim() ?? "";
        if (string.IsNullOrEmpty(target) || target == "…")
            return source;
        if (string.IsNullOrEmpty(source) || string.Equals(source, target, StringComparison.Ordinal))
            return target;

        return $"{target}{Environment.NewLine}{Environment.NewLine}{source}";
    }
}
