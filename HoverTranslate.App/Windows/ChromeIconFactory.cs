using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HoverTranslate.App.Windows;

internal enum ChromeIconKind
{
    Minimize,
    Maximize,
    Restore,
    Close
}

internal static class ChromeIconFactory
{
    private static readonly SolidColorBrush StrokeBrush = new(
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")!);

    public static UIElement Create(ChromeIconKind kind)
    {
        var path = new Path
        {
            Stroke = StrokeBrush,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            SnapsToDevicePixels = true,
            Fill = System.Windows.Media.Brushes.Transparent,
            Stretch = Stretch.None,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        path.Data = kind switch
        {
            ChromeIconKind.Minimize => Geometry.Parse("M1,5.5 H9"),
            ChromeIconKind.Maximize => Geometry.Parse("M1.5,1.5 H8.5 V8.5 H1.5 Z"),
            ChromeIconKind.Restore => Geometry.Parse("M1.5,3.5 H6.5 V8.5 H1.5 Z M3.5,1.5 H8.5 V6.5 H3.5 Z"),
            ChromeIconKind.Close => Geometry.Parse("M1.5,1.5 L8.5,8.5 M8.5,1.5 L1.5,8.5"),
            _ => Geometry.Parse("M1,5.5 H9")
        };

        return new Viewbox
        {
            Width = 10,
            Height = 10,
            Child = path
        };
    }
}
