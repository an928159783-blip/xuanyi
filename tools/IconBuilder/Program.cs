using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

if (args.Length >= 2 && args[0] == "--brand")
{
    var icoPath = Path.GetFullPath(args[1]);
    Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);
    var sizes = new[] { 16, 32, 48, 64, 128, 256 };
    var images = sizes.Select(RenderBrandIcon).ToArray();
    WriteIco(icoPath, images);
    foreach (var img in images) img.Dispose();
    Console.WriteLine($"Wrote brand icon: {icoPath}");
    return 0;
}

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: IconBuilder --brand <output.ico>  OR  IconBuilder <input.png> <output.ico>");
    return 1;
}

var pngPath = Path.GetFullPath(args[0]);
var outIco = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(outIco)!);

using var source = MakeTransparentBackground(new Bitmap(pngPath));
var sizeList = new[] { 16, 32, 48, 64, 128, 256 };
var bitmaps = sizeList.Select(s => ResizeArgb(s, source)).ToArray();
WriteIco(outIco, bitmaps);
foreach (var img in bitmaps) img.Dispose();
source.Dispose();
Console.WriteLine($"Wrote {outIco}");
return 0;

static Bitmap RenderBrandIcon(int size)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    g.Clear(Color.Transparent);

    var pad = size * 0.06f;
    var rect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);
    using (var bgPath = RoundedRect(rect, size * 0.26f))
    {
        using (var bgBrush = new LinearGradientBrush(rect,
                   Color.FromArgb(255, 219, 234, 254), Color.FromArgb(255, 255, 255, 255), 90f))
            g.FillPath(bgBrush, bgPath);

        using (var borderPen = new Pen(Color.FromArgb(255, 191, 219, 254), Math.Max(1f, size * 0.02f)))
            g.DrawPath(borderPen, bgPath);
    }

    var fontFamily = PickFontFamily();
    using var fontYi = new Font(fontFamily, size * 0.38f, FontStyle.Bold, GraphicsUnit.Pixel);
    using var ink = new SolidBrush(Color.FromArgb(255, 15, 23, 42));

    var yi = "译";
    var yiSize = g.MeasureString(yi, fontYi);
    var yiX = rect.X + (rect.Width - yiSize.Width) / 2f;
    var yiY = rect.Y + (rect.Height - yiSize.Height) / 2f + size * 0.01f;
    g.DrawString(yi, fontYi, ink, yiX, yiY);

    return bmp;
}

static string PickFontFamily()
{
    var installed = new InstalledFontCollection().Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var name in new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Arial Unicode MS" })
    {
        if (installed.Contains(name))
            return name;
    }
    return FontFamily.GenericSansSerif.Name;
}

static GraphicsPath RoundedRect(RectangleF bounds, float radius)
{
    var path = new GraphicsPath();
    var d = radius * 2;
    path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
    path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
    path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
    path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
    path.CloseFigure();
    return path;
}

static Bitmap MakeTransparentBackground(Bitmap src)
{
    var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
    for (var y = 0; y < src.Height; y++)
    for (var x = 0; x < src.Width; x++)
    {
        var c = src.GetPixel(x, y);
        if (c.A > 10 && c.R > 235 && c.G > 235 && c.B > 235)
            c = Color.FromArgb(0, c);
        else if (c.A > 10 && Math.Abs(c.R - c.G) < 18 && Math.Abs(c.G - c.B) < 18 && c.R > 200)
            c = Color.FromArgb(0, c);
        bmp.SetPixel(x, y, c);
    }
    return bmp;
}

static Bitmap ResizeArgb(int size, Bitmap source)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.Clear(Color.Transparent);
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.DrawImage(source, 0, 0, size, size);
    return bmp;
}

static void WriteIco(string path, Bitmap[] images)
{
    using var fs = File.Create(path);
    using var w = new BinaryWriter(fs);
    w.Write((ushort)0);
    w.Write((ushort)1);
    w.Write((ushort)images.Length);
    var offset = 6 + 16 * images.Length;
    var blobs = images.Select(ToIcoBitmapBytes).ToArray();
    foreach (var (blob, i) in blobs.Select((b, i) => (b, i)))
    {
        var size = images[i].Width;
        w.Write((byte)(size >= 256 ? 0 : size));
        w.Write((byte)(size >= 256 ? 0 : size));
        w.Write((byte)0);
        w.Write((byte)0);
        w.Write((ushort)1);
        w.Write((ushort)32);
        w.Write(blob.Length);
        w.Write(offset);
        offset += blob.Length;
    }
    foreach (var blob in blobs) w.Write(blob);
}

static byte[] ToIcoBitmapBytes(Bitmap image)
{
    var w = image.Width;
    var h = image.Height;
    var xorStride = ((w * 4 + 3) / 4) * 4;
    var xorSize = xorStride * h;
    var andStride = ((w + 31) / 32) * 4;
    var andSize = andStride * h;
    using var ms = new MemoryStream(40 + xorSize + andSize);
    using var bw = new BinaryWriter(ms);
    bw.Write(40);
    bw.Write(w);
    bw.Write(h * 2);
    bw.Write((ushort)1);
    bw.Write((ushort)32);
    bw.Write(0);
    bw.Write(xorSize + andSize);
    bw.Write(0);
    bw.Write(0);
    bw.Write(0);
    bw.Write(0);
    var rect = new Rectangle(0, 0, w, h);
    var data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try
    {
        var row = new byte[xorStride];
        for (var y = h - 1; y >= 0; y--)
        {
            Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, w * 4);
            bw.Write(row.AsSpan(0, w * 4));
        }
    }
    finally { image.UnlockBits(data); }
    bw.Write(new byte[andSize]);
    return ms.ToArray();
}
