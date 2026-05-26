using System.Windows;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Windows;

internal static class PanelPlacementService
{
    public static void Apply(Window window, PanelPlacement placement, Action defaultPosition)
    {
        if (placement.Left is double left && placement.Top is double top)
        {
            window.Left = left;
            window.Top = top;
            ClampToWorkArea(window);
        }
        else
        {
            defaultPosition();
        }

        if (placement.Width is > 100)
            window.Width = placement.Width.Value;
        if (placement.Height is > 80)
            window.Height = placement.Height.Value;
    }

    public static void Remember(Window window, PanelPlacement placement)
    {
        if (!window.IsVisible) return;

        placement.Left = window.Left;
        placement.Top = window.Top;
        placement.Width = window.Width;
        placement.Height = window.Height;
    }

    private static void ClampToWorkArea(Window window)
    {
        var area = SystemParameters.WorkArea;
        if (window.Left < area.Left) window.Left = area.Left;
        if (window.Top < area.Top) window.Top = area.Top;
        if (window.Left + window.Width > area.Right)
            window.Left = Math.Max(area.Left, area.Right - window.Width);
        if (window.Top + window.Height > area.Bottom)
            window.Top = Math.Max(area.Top, area.Bottom - window.Height);
    }
}
