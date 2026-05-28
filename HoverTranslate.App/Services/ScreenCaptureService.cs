using System.Drawing;
using System.Drawing.Imaging;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Services;

public static class ScreenCaptureService
{
    public static Bitmap Capture(ScreenshotRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0)
            throw new ArgumentException("截屏区域宽高必须大于 0。");

        var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
        return bmp;
    }
}
