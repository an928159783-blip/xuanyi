namespace HoverTranslate.App.Windows;

internal static class PanelLayout
{
    private const double Margin = 16;
    private const double Gap = 10;

    public const double DefaultTranslationWidth = 360;
    public const double DefaultTranslationHeight = 180;
    public const double DefaultHistoryWidth = 360;
    public const double DefaultHistoryHeight = 400;

    public static void ResetTranslationWindow(System.Windows.Window window)
    {
        DefaultTranslationPosition(window);
        window.Width = DefaultTranslationWidth;
        window.Height = DefaultTranslationHeight;
    }

    public static void ResetHistoryWindow(System.Windows.Window window)
    {
        DefaultHistoryPosition(window);
        window.Width = DefaultHistoryWidth;
        var area = System.Windows.SystemParameters.WorkArea;
        window.Height = Math.Min(520, Math.Max(DefaultHistoryHeight, area.Height - 80));
    }

    public static void DefaultTranslationPosition(System.Windows.Window window)
    {
        var area = System.Windows.SystemParameters.WorkArea;
        window.Left = area.Right - DefaultTranslationWidth - Margin;
        window.Top = area.Top + 48;
    }

    public static void DefaultHistoryPosition(System.Windows.Window window)
    {
        var area = System.Windows.SystemParameters.WorkArea;
        window.Left = area.Right - DefaultTranslationWidth - Gap - DefaultHistoryWidth - Margin;
        window.Top = area.Top + 48;
    }
}
