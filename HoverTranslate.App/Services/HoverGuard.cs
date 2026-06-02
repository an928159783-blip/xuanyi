namespace HoverTranslate.App.Services;

/// <summary>供 UI 层在用户关闭译文窗等场景暂停悬停/选中 tick。</summary>
internal static class HoverGuard
{
    private static Action<int>? _pauseHandler;

    public static void RegisterPauseHandler(Action<int> handler) => _pauseHandler = handler;

    public static void PauseFor(int milliseconds) => _pauseHandler?.Invoke(milliseconds);
}
