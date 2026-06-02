using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HoverTranslate.App.Services;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

internal static class PanelAppearance
{
    public static string ResolveTheme(string? theme) =>
        theme?.ToLowerInvariant() switch
        {
            "system" => SystemThemeHelper.IsAppsLightTheme() ? "light" : "dark",
            "light" or "slate" or "dark" => theme,
            _ => "dark"
        };

    public static System.Windows.Media.FontFamily ResolveFontFamily(string? fontFamilyName)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyName))
            return new System.Windows.Media.FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");

        try
        {
            return new System.Windows.Media.FontFamily(fontFamilyName.Trim());
        }
        catch
        {
            return new System.Windows.Media.FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
        }
    }

    public const double OpacityMin = 0.0;
    public const double OpacityMax = 1.0;
    public const double OpacityDefault = 0.92;

    public static double ClampOpacity(double value) => Math.Clamp(value, OpacityMin, OpacityMax);

    public static int ToPercent(double opacity) => (int)Math.Round(ClampOpacity(opacity) * 100);

    public static double FromPercent(int percent) => ClampOpacity(percent / 100.0);

    public static (System.Windows.Media.Color Background, System.Windows.Media.Color Border, System.Windows.Media.Color Text, System.Windows.Media.Color SubText) GetTheme(string? theme) =>
        ResolveTheme(theme) switch
        {
            "light" => (Parse("#F8FAFC"), Parse("#CBD5E1"), Parse("#1E293B"), Parse("#64748B")),
            "slate" => (Parse("#334155"), Parse("#475569"), Parse("#F1F5F9"), Parse("#94A3B8")),
            _ => (Parse("#1E1E2E"), Parse("#313244"), Parse("#CDD6F4"), Parse("#A6ADC8"))
        };

    public static void ApplyPanel(Window window, Border root, PanelChromeOptions ui, string? theme)
    {
        window.Opacity = 1.0;
        window.Background = System.Windows.Media.Brushes.Transparent;
        var opacity = ClampOpacity(ui.Opacity);
        var colors = GetTheme(theme);
        var bgAlpha = ToBackgroundAlpha(opacity);
        root.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(bgAlpha, colors.Background.R, colors.Background.G, colors.Background.B));
        var borderAlpha = (byte)Math.Clamp((int)Math.Round(bgAlpha * 0.72), 0, 255);
        root.BorderBrush = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(borderAlpha, colors.Border.R, colors.Border.G, colors.Border.B));
        root.Opacity = 1.0;
    }

    public static byte ToBackgroundAlpha(double opacity) =>
        (byte)Math.Clamp((int)Math.Round(ClampOpacity(opacity) * 255), 0, 255);

    private static System.Windows.Media.Color Parse(string hex) =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
}
