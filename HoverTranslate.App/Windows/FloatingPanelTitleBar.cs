using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelTitleBar
{
    public static void Wire(
        Window window,
        UIElement dragHandle,
        System.Windows.Controls.Button minimizeButton,
        System.Windows.Controls.Button maximizeButton,
        System.Windows.Controls.Button closeButton,
        Action onMinimize,
        Action onMaximize,
        Action onClose)
    {
        dragHandle.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                onMaximize();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { window.DragMove(); } catch { /* ignore */ }
            }
        };

        minimizeButton.Click += (_, _) => onMinimize();
        maximizeButton.Click += (_, _) => onMaximize();
        closeButton.Click += (_, _) => onClose();

        System.Windows.Controls.Panel.SetZIndex(minimizeButton, 20);
        System.Windows.Controls.Panel.SetZIndex(maximizeButton, 20);
        System.Windows.Controls.Panel.SetZIndex(closeButton, 20);
    }
}
