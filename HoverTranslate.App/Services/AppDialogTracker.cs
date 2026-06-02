namespace HoverTranslate.App.Services;

/// <summary>炫译自有对话框打开计数，用于抑制选中即译/悬停误触。</summary>
internal static class AppDialogTracker
{
    private static int _openCount;

    public static bool IsAnyOpen => _openCount > 0;

    public static void RegisterOpened() => Interlocked.Increment(ref _openCount);

    public static void RegisterClosed() => Interlocked.Decrement(ref _openCount);
}
