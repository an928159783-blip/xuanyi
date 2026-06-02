using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace HoverTranslate.App.Services;

public sealed class OcrService
{
    private OcrEngine? _engine;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (_engine is not null) return;

        _engine = OcrEngine.TryCreateFromUserProfileLanguages()
                  ?? OcrEngine.TryCreateFromLanguage(new global::Windows.Globalization.Language("en-US"))
                  ?? OcrEngine.TryCreateFromLanguage(new global::Windows.Globalization.Language("zh-Hans"));
        IsAvailable = _engine is not null;
        await Task.CompletedTask;
    }

    public async Task<string?> RecognizeAsync(Bitmap bitmap, CancellationToken cancellationToken = default)
    {
        if (_engine is null)
            await InitializeAsync().ConfigureAwait(false);

        if (_engine is null)
            return null;

        using var prepared = PrepareForOcr(bitmap);
        using var softwareBitmap = await ToSoftwareBitmapAsync(prepared).ConfigureAwait(false);
        var result = await _engine.RecognizeAsync(softwareBitmap).AsTask(cancellationToken).ConfigureAwait(false);
        if (result.Lines.Count == 0)
            return null;

        return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text)).Trim();
    }

    /// <summary>小区域（如单词）放大后再 OCR，提高 Windows OCR 识别率。</summary>
    private static Bitmap PrepareForOcr(Bitmap source)
    {
        const int minSide = 72;
        var w = source.Width;
        var h = source.Height;
        if (w <= 0 || h <= 0)
            return (Bitmap)source.Clone();

        if (w >= minSide && h >= minSide)
            return (Bitmap)source.Clone();

        var scale = Math.Max((double)minSide / w, (double)minSide / h);
        scale = Math.Min(scale, 5.0);
        var nw = Math.Max(1, (int)Math.Ceiling(w * scale));
        var nh = Math.Max(1, (int)Math.Ceiling(h * scale));

        var scaled = new Bitmap(nw, nh, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, nw, nh);
        }

        return scaled;
    }

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        using var randomAccessStream = new InMemoryRandomAccessStream();
        await randomAccessStream.WriteAsync(stream.ToArray().AsBuffer());
        randomAccessStream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
