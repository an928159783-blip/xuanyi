using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using HoverTranslate.App.Windows;

namespace HoverTranslate.App.Services;

/// <summary>鼠标在炫译自有浮窗上时不做悬停取词/翻译。</summary>
internal static class AppWindowHoverGuard
{
    private static readonly Type[] SuppressedWindowTypes =
    [
        typeof(TranslationResultWindow),
        typeof(HistoryPanelWindow),
        typeof(SettingsWindow),
        typeof(RegionSelectorOverlayWindow),
        typeof(FeatureGuideWindow),
        typeof(AppDialogWindow),
        typeof(StartupNoticeWindow),
        typeof(OverlayWindow)
    ];

    /// <summary>前台为本应用（设置/对话框/浮窗）时，不触发选中即译。</summary>
    public static bool ShouldSuppressAutoTranslateCapture()
    {
        if (ShouldSuppressHoverCapture())
            return true;

        if (SettingsWindowHost.IsOpen || AppDialogTracker.IsAnyOpen)
            return true;

        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        return pid == Environment.ProcessId;
    }

    public static bool ShouldSuppressHoverCapture()
    {
        if (!GetCursorPos(out var p))
            return false;

        var app = System.Windows.Application.Current;
        if (app is null)
            return false;

        // 透明 Topmost 浮窗常让 WindowFromPoint 穿透到下层；先按窗口外接矩形判断。
        foreach (Window window in app.Windows)
        {
            if (!IsSuppressedWindowType(window))
                continue;

            if (!IsWindowOnScreen(window))
                continue;

            if (IsScreenPointInWindowBounds(window, p.X, p.Y))
                return true;
        }

        var hwndUnder = WindowFromPoint(p);
        if (hwndUnder != IntPtr.Zero && IsHwndOwnedByApp(hwndUnder))
            return true;

        return false;
    }

    private static bool IsWindowOnScreen(Window window) =>
        window.IsVisible
        && window.Visibility == Visibility.Visible
        && window.ActualWidth > 0
        && window.ActualHeight > 0;

    private static bool IsSuppressedWindowType(Window window)
    {
        var t = window.GetType();
        foreach (var allowed in SuppressedWindowTypes)
        {
            if (allowed.IsInstanceOfType(window))
                return true;
        }

        return false;
    }

    private static bool IsScreenPointInWindowBounds(Window window, int screenX, int screenY)
    {
        if (PresentationSource.FromVisual(window) is HwndSource { Handle: var hwnd }
            && GetWindowRect(hwnd, out var rect)
            && screenX >= rect.Left && screenX < rect.Right
            && screenY >= rect.Top && screenY < rect.Bottom)
        {
            return true;
        }

        try
        {
            var local = window.PointFromScreen(new System.Windows.Point(screenX, screenY));
            return local.X >= 0 && local.Y >= 0
                   && local.X <= window.ActualWidth
                   && local.Y <= window.ActualHeight;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsHwndOwnedByApp(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out var pid);
        if (pid != Environment.ProcessId)
            return false;

        var app = System.Windows.Application.Current;
        if (app is null) return false;

        var root = GetAncestor(hwnd, GaRoot);
        foreach (Window window in app.Windows)
        {
            if (!IsSuppressedWindowType(window))
                continue;

            if (PresentationSource.FromVisual(window) is HwndSource { Handle: var wh })
            {
                if (root == wh || IsDescendant(hwnd, wh))
                    return true;
            }
        }

        return false;
    }

    private static bool IsDescendant(IntPtr child, IntPtr ancestor)
    {
        var current = child;
        while (current != IntPtr.Zero)
        {
            if (current == ancestor)
                return true;
            current = GetParent(current);
        }

        return false;
    }

    private const uint GaRoot = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
