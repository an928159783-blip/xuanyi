using System.Drawing;
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
                  ?? OcrEngine.TryCreateFromLanguage(new global::Windows.Globalization.Language("zh-Hans"))
                  ?? OcrEngine.TryCreateFromLanguage(new global::Windows.Globalization.Language("en-US"));
        IsAvailable = _engine is not null;
        await Task.CompletedTask;
    }

    public async Task<string?> RecognizeAsync(Bitmap bitmap, CancellationToken cancellationToken = default)
    {
        if (_engine is null)
            await InitializeAsync().ConfigureAwait(false);

        if (_engine is null)
            return null;

        using var softwareBitmap = await ToSoftwareBitmapAsync(bitmap).ConfigureAwait(false);
        var result = await _engine.RecognizeAsync(softwareBitmap).AsTask(cancellationToken).ConfigureAwait(false);
        if (result.Lines.Count == 0)
            return null;

        return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text)).Trim();
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
