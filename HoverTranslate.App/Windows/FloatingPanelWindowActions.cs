using System.Windows;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelWindowActions
{
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
