using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HoverTranslate.App.Windows;

internal enum ResizeEdge
{
    Right,
    Bottom,
    BottomRight
}

internal static class WindowResizeHelper
{
    public static void Wire(Border grip, Window window, ResizeEdge edge)
    {
        grip.Background = System.Windows.Media.Brushes.Transparent;
        grip.Cursor = edge switch
        {
            ResizeEdge.Right => System.Windows.Input.Cursors.SizeWE,
            ResizeEdge.Bottom => System.Windows.Input.Cursors.SizeNS,
            _ => System.Windows.Input.Cursors.SizeNWSE
        };
        if (edge == ResizeEdge.BottomRight)
            grip.ToolTip = "拖动调整大小";

        grip.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            new ResizeSession(window, edge, grip).Begin(e);
        };
    }

    public static void PlaceOnGrid(Grid host, params Border[] grips)
    {
        var span = Math.Max(1, host.RowDefinitions.Count);
        foreach (var grip in grips)
        {
            Grid.SetRow(grip, 0);
            Grid.SetRowSpan(grip, span);
            Grid.SetColumn(grip, 0);
            System.Windows.Controls.Panel.SetZIndex(grip, 500);
            if (!host.Children.Contains(grip))
                host.Children.Add(grip);
        }
    }

    private sealed class ResizeSession
    {
        private readonly Window _window;
        private readonly ResizeEdge _edge;
        private readonly UIElement _captureTarget;
        private System.Windows.Point _startMouse;
        private double _startW;
        private double _startH;

        public ResizeSession(Window window, ResizeEdge edge, UIElement captureTarget)
        {
            _window = window;
            _edge = edge;
            _captureTarget = captureTarget;
        }

        public void Begin(MouseButtonEventArgs e)
        {
            _startMouse = Mouse.GetPosition(_window);
            _startW = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
            _startH = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;

            _captureTarget.CaptureMouse();
            _captureTarget.MouseMove += OnMove;
            _window.PreviewMouseLeftButtonUp += OnUp;
        }

        private void OnMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                End();
                return;
            }

            var pos = Mouse.GetPosition(_window);
            var dx = pos.X - _startMouse.X;
            var dy = pos.Y - _startMouse.Y;

            if (_edge is ResizeEdge.Right or ResizeEdge.BottomRight)
                _window.Width = Math.Max(_window.MinWidth, _startW + dx);
            if (_edge is ResizeEdge.Bottom or ResizeEdge.BottomRight)
                _window.Height = Math.Max(_window.MinHeight, _startH + dy);
        }

        private void OnUp(object sender, MouseButtonEventArgs e) => End();

        private void End()
        {
            _captureTarget.MouseMove -= OnMove;
            _window.PreviewMouseLeftButtonUp -= OnUp;
            if (Mouse.Captured == _captureTarget)
                _captureTarget.ReleaseMouseCapture();
        }
    }
}
