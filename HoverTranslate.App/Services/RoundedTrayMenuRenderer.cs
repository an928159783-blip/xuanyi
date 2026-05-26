using System.Drawing;
using System.Drawing.Drawing2D;

namespace HoverTranslate.App.Services;

/// <summary>圆角浅色托盘菜单（接近 Win11 / Cursor 风格）</summary>
internal sealed class RoundedTrayMenuRenderer : ToolStripProfessionalRenderer
{
    private const int Radius = 10;

    public RoundedTrayMenuRenderer() : base(new RoundedTrayColorTable()) { }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        using var path = CreateRoundRect(rect, Radius);
        using var brush = new SolidBrush(Color.FromArgb(252, 252, 253));
        g.FillPath(brush, path);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        using var path = CreateRoundRect(rect, Radius);
        using var pen = new Pen(Color.FromArgb(210, 214, 222));
        g.DrawPath(pen, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var color = Color.FromArgb(30, 41, 59);
        var rect = e.TextRectangle;
        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            e.TextFont,
            rect,
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var r = e.Item.ContentRectangle;
        var y = r.Top + r.Height / 2;
        using var pen = new Pen(Color.FromArgb(226, 232, 240));
        e.Graphics.DrawLine(pen, r.Left + 12, y, r.Right - 12, y);
    }

    internal static void ApplyRoundedRegion(ContextMenuStrip menu)
    {
        using var path = CreateRoundRect(new Rectangle(0, 0, menu.Width, menu.Height), Radius);
        menu.Region = new Region(path);
    }

    private static GraphicsPath CreateRoundRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class RoundedTrayColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(241, 245, 249);
        public override Color MenuItemBorder => Color.FromArgb(241, 245, 249);
        public override Color ToolStripDropDownBackground => Color.FromArgb(252, 252, 253);
        public override Color ImageMarginGradientBegin => Color.FromArgb(252, 252, 253);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(252, 252, 253);
        public override Color ImageMarginGradientEnd => Color.FromArgb(252, 252, 253);
    }
}
