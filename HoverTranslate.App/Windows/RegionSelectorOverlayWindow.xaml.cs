using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

public partial class RegionSelectorOverlayWindow : Window
{
    private System.Windows.Point _start;
    private bool _dragging;
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;

    public RegionSelectorOverlayWindow()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        Loaded += (_, _) =>
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            _dpiScaleX = dpi.DpiScaleX;
            _dpiScaleY = dpi.DpiScaleY;
            Focus();
        };
        KeyDown += OnKeyDown;
        RootCanvas.MouseLeftButtonDown += OnMouseDown;
        RootCanvas.MouseMove += OnMouseMove;
        RootCanvas.MouseLeftButtonUp += OnMouseUp;
    }

    public static Task<ScreenshotRegion?> PickAsync()
    {
        var tcs = new TaskCompletionSource<ScreenshotRegion?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var w = new RegionSelectorOverlayWindow();
        w.Confirmed += r =>
        {
            if (tcs.TrySetResult(r))
                w.Close();
        };
        w.Cancelled += () =>
        {
            tcs.TrySetResult(null);
            w.Close();
        };
        w.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.TrySetResult(null);
        };
        w.Show();
        w.Dispatcher.BeginInvoke(() => w.Activate(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        return tcs.Task;
    }

    public event Action<ScreenshotRegion>? Confirmed;
    public event Action? Cancelled;

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancelled?.Invoke();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            TryConfirm();
            e.Handled = true;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _dragging = true;
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, _start.X);
        Canvas.SetTop(SelectionBorder, _start.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
        RootCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(RootCanvas);
        var x = Math.Min(_start.X, pos.X);
        var y = Math.Min(_start.Y, pos.Y);
        var w = Math.Abs(pos.X - _start.X);
        var h = Math.Abs(pos.Y - _start.Y);
        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        RootCanvas.ReleaseMouseCapture();
        TryConfirm();
    }

    private void TryConfirm()
    {
        if (SelectionBorder.Visibility != Visibility.Visible
            || SelectionBorder.Width < 4
            || SelectionBorder.Height < 4)
            return;

        var left = Canvas.GetLeft(SelectionBorder);
        var top = Canvas.GetTop(SelectionBorder);
        var w = SelectionBorder.Width;
        var h = SelectionBorder.Height;

        var physicalX = (int)Math.Round((SystemParameters.VirtualScreenLeft + left) * _dpiScaleX);
        var physicalY = (int)Math.Round((SystemParameters.VirtualScreenTop + top) * _dpiScaleY);
        var physicalW = Math.Max(1, (int)Math.Round(w * _dpiScaleX));
        var physicalH = Math.Max(1, (int)Math.Round(h * _dpiScaleY));

        Confirmed?.Invoke(new ScreenshotRegion
        {
            Name = "pick",
            X = physicalX,
            Y = physicalY,
            Width = physicalW,
            Height = physicalH
        });
    }
}
