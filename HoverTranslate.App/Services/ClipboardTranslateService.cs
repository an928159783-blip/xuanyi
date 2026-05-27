using System.Runtime.InteropServices;
using System.Windows.Interop;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

/// <summary>开启后：用户 Ctrl+C 复制选中文本时自动触发翻译（无需再按翻译热键）。</summary>
public sealed class ClipboardTranslateService : IDisposable
{
    private const int WmClipboardUpdate = 0x031D;

    private HwndSource? _hwndSource;
    private ConfigService? _configService;
    private Action<string>? _onTranslate;
    private string? _lastTriggered;
    private DateTime _lastAt = DateTime.MinValue;
    private DateTime _pausedUntil = DateTime.MinValue;

    public void PauseFor(int milliseconds) =>
        _pausedUntil = DateTime.UtcNow.AddMilliseconds(milliseconds);

    public void Start(ConfigService configService, Action<string> onTranslate)
    {
        _configService = configService;
        _onTranslate = onTranslate;

        if (_hwndSource is not null) return;

        var parameters = new HwndSourceParameters("XuanYiClipboardHost")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0x08000000 | 0x00000080
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        NativeMethods.AddClipboardFormatListener(_hwndSource.Handle);
    }

    public void Stop()
    {
        if (_hwndSource is not null)
        {
            NativeMethods.RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmClipboardUpdate) return IntPtr.Zero;
        if (DateTime.UtcNow < _pausedUntil) return IntPtr.Zero;

        var config = _configService?.Load();
        if (config is not { TranslateOnCopy: true })
            return IntPtr.Zero;

        try
        {
            if (!Clipboard.ContainsText()) return IntPtr.Zero;
            var text = Clipboard.GetText()?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
                return IntPtr.Zero;

            if (!LooksLikeTranslatable(text))
                return IntPtr.Zero;

            var now = DateTime.UtcNow;
            if (text == _lastTriggered && (now - _lastAt).TotalMilliseconds < 800)
                return IntPtr.Zero;

            _lastTriggered = text;
            _lastAt = now;
            _onTranslate?.Invoke(text);
        }
        catch
        {
            // ignore clipboard races
        }

        return IntPtr.Zero;
    }

    private static bool LooksLikeTranslatable(string text)
    {
        var letters = text.Count(char.IsLetter);
        if (letters < 2) return false;
        var latin = text.Count(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        return latin >= Math.Max(2, letters / 3);
    }

    public void Dispose() => Stop();
}
