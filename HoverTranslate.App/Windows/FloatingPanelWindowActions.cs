using System.Windows;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelWindowActions
{
    /// <summary>应用退出时设为 true，允许面板窗真正 Close。</summary>
    public static bool AllowPanelWindowsToClose { get; set; }

    public static void WireHideInsteadOfClose(Window window, Action onHide)
    {
        window.Closed += (_, _) => ClearSingletonIfMatches(window);
        window.Closing += (_, e) =>
        {
            if (AllowPanelWindowsToClose)
                return;

            e.Cancel = true;
            onHide();
        };
    }

    private static void ClearSingletonIfMatches(Window window)
    {
        if (window is HistoryPanelWindow)
            HistoryPanelWindow.ResetInstanceIfClosed(window);
        else if (window is TranslationResultWindow)
            TranslationResultWindow.ResetInstanceIfClosed(window);
    }

    public static void HidePanel(Window window, Action rememberPlacement)
    {
        rememberPlacement();
        window.WindowState = WindowState.Normal;
        window.ShowInTaskbar = false;
        window.Hide();
        window.Visibility = Visibility.Hidden;
    }

    public static void Minimize(Window window, Action rememberPlacement)
    {
        rememberPlacement();
        window.ShowInTaskbar = true;
        window.Visibility = Visibility.Visible;
        window.Show();
        window.WindowState = WindowState.Minimized;
    }

    public static void RestoreFromTaskbar(Window window)
    {
        window.WindowState = WindowState.Normal;
        window.Visibility = Visibility.Visible;
        window.Show();
        window.Activate();
        window.ShowInTaskbar = false;
    }
}
