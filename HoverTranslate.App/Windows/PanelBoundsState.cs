using System.Windows;
using System.Windows.Controls;

namespace HoverTranslate.App.Windows;

internal sealed class PanelBoundsState
{
    public bool IsMaximized { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

internal static class PanelBoundsActions
{
    public static void ToggleMaximize(Window window, PanelBoundsState state, System.Windows.Controls.Button maxButton)
    {
        if (state.IsMaximized)
        {
            window.Left = state.Left;
            window.Top = state.Top;
            window.Width = state.Width;
            window.Height = state.Height;
            state.IsMaximized = false;
            FloatingPanelChrome.ApplyTitleBarButton(maxButton, ChromeIconKind.Maximize, "最大化（铺满工作区）");
            return;
        }

        state.Left = window.Left;
        state.Top = window.Top;
        state.Width = window.Width;
        state.Height = window.Height;

        var area = SystemParameters.WorkArea;
        window.Left = area.Left + 8;
        window.Top = area.Top + 8;
        window.Width = Math.Max(window.MinWidth, area.Width - 16);
        window.Height = Math.Max(window.MinHeight, area.Height - 16);
        state.IsMaximized = true;
        FloatingPanelChrome.ApplyTitleBarButton(maxButton, ChromeIconKind.Restore, "还原窗口大小");
    }
}
