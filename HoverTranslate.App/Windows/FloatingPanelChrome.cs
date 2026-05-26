using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelChrome
{
    public static void ApplyAppearance(Window window, Border root, PanelChromeOptions options,
        TextBlock? primaryText = null, TextBlock? secondaryText = null)
    {
        PanelAppearance.ApplyPanel(window, root, options, options.Theme);
        if (primaryText is not null)
        {
            var colors = PanelAppearance.GetTheme(options.Theme);
            primaryText.Foreground = new SolidColorBrush(colors.Text);
            if (secondaryText is not null)
                secondaryText.Foreground = new SolidColorBrush(colors.SubText);
        }
    }

    public static void ApplyTitleBarButton(System.Windows.Controls.Button button, ChromeIconKind kind, string toolTip)
    {
        button.Width = 32;
        button.Height = 24;
        button.Padding = new Thickness(0);
        button.Margin = new Thickness(0);
        button.Content = ChromeIconFactory.Create(kind);
        button.Background = System.Windows.Media.Brushes.Transparent;
        button.BorderThickness = new Thickness(0);
        button.Cursor = System.Windows.Input.Cursors.Hand;
        button.ToolTip = toolTip;
        button.VerticalAlignment = VerticalAlignment.Center;
        button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
        button.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        button.Focusable = false;
    }

    public static void WireHiddenScroll(ScrollViewer scroll)
    {
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        scroll.PreviewMouseWheel += (_, e) =>
        {
            var next = scroll.VerticalOffset - e.Delta;
            next = Math.Max(0, Math.Min(scroll.ScrollableHeight, next));
            scroll.ScrollToVerticalOffset(next);
            e.Handled = true;
        };
    }

    public static void WireAllScrollViewers(DependencyObject root)
    {
        if (root is ScrollViewer sv)
            WireHiddenScroll(sv);

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
            WireAllScrollViewers(VisualTreeHelper.GetChild(root, i));
    }
}
