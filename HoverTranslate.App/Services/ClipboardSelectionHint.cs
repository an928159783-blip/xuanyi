namespace HoverTranslate.App.Services;

/// <summary>记录近期剪贴板文本，供选中即译在 UIA 读不到选区时兜底。</summary>
internal static class ClipboardSelectionHint
{
    private static string? _lastKnown;
    private static DateTime _lastKnownAt = DateTime.MinValue;

    public static void NoteClipboardText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _lastKnown = text.Trim();
        _lastKnownAt = DateTime.UtcNow;
    }

    public static string? TryGetRecent(int maxAgeMs = 5000)
    {
        if (_lastKnown is null) return null;
        if ((DateTime.UtcNow - _lastKnownAt).TotalMilliseconds > maxAgeMs) return null;
        return _lastKnown;
    }

    public static string? TryGetFreshFromClipboard(int maxAgeMs = 5000)
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    NoteClipboardText(text);
                    return text;
                }
            }
        }
        catch
        {
            // ignore clipboard races
        }

        return TryGetRecent(maxAgeMs);
    }
}
