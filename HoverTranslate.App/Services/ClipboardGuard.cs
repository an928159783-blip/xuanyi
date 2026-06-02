namespace HoverTranslate.App.Services;

/// <summary>写入剪贴板时短暂抑制「复制后自动翻译」，避免历史/译文窗复制触发误译。</summary>
public static class ClipboardGuard
{
    private static Action<int>? _pauseClipboardTranslate;

    public static void RegisterPauseHandler(Action<int> pauseMs) =>
        _pauseClipboardTranslate = pauseMs;

    public static void SetText(string text, int suppressAutoTranslateMs = 6000)
    {
        if (!string.IsNullOrEmpty(text))
            _pauseClipboardTranslate?.Invoke(suppressAutoTranslateMs);
        Clipboard.SetText(text);
    }
}
