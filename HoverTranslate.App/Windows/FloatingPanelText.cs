using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelText
{
    public static void ApplyReadOnlyBox(System.Windows.Controls.TextBox box, PanelChromeOptions options, bool primary)
    {
        var colors = PanelAppearance.GetTheme(options.Theme);
        box.FontFamily = PanelAppearance.ResolveFontFamily(options.FontFamilyName);
        var baseSize = primary ? 14.0 : 12.0;
        box.FontSize = Math.Round(baseSize * options.FontSizeScale, 1);
        box.Foreground = new SolidColorBrush(colors.Text);
        box.CaretBrush = box.Foreground;
        box.Background = System.Windows.Media.Brushes.Transparent;
        box.BorderThickness = new Thickness(0);
        box.IsReadOnly = true;
        box.IsReadOnlyCaretVisible = true;
        box.IsTabStop = true;
        box.Focusable = true;
        box.AcceptsReturn = true;
        box.TextWrapping = TextWrapping.Wrap;
        box.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        box.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        box.Padding = new Thickness(0);
        box.Margin = primary ? new Thickness(0) : new Thickness(0, 0, 0, 6);
    }
}
