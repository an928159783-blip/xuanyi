using System.Runtime.InteropServices;
using System.Windows.Threading;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class HoverTranslateService : IDisposable
{
    private readonly TranslationCoordinator _coordinator;
    private readonly ConfigService _configService;
    private readonly DispatcherTimer _timer;

    private int _lastX = -1, _lastY = -1;
    private string? _stableText;
    private int _textStillMs;
    private bool _busy;
    private DateTime _pausedUntil = DateTime.MinValue;

    public HoverTranslateService(TranslationCoordinator coordinator, ConfigService configService)
    {
        _coordinator = coordinator;
        _configService = configService;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (!_configService.Load().EnableHover) return;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void PauseFor(int milliseconds) =>
        _pausedUntil = DateTime.UtcNow.AddMilliseconds(milliseconds);

    /// <summary>热键/手动翻译开始时：暂停悬停，避免用光标下单个词覆盖整段选中翻译。</summary>
    public void OnManualTranslateStarting()
    {
        _pausedUntil = DateTime.UtcNow.AddSeconds(15);
        _busy = true;
        _stableText = null;
        _textStillMs = 0;
        _lastX = -1;
        _lastY = -1;
    }

    public void OnManualTranslateFinished() => _busy = false;

    private void OnTick(object? sender, EventArgs e)
    {
        if (_busy || DateTime.UtcNow < _pausedUntil) return;

        var config = _configService.Load();
        if (!config.EnableHover) return;

        if (IsLeftButtonDown()) return;

        if (UiAutomationSelectionCapture.HasActiveSelection())
            return;

        var pos = GetCursorPos();
        var moved = pos.X != _lastX || pos.Y != _lastY;
        if (moved)
        {
            _lastX = pos.X;
            _lastY = pos.Y;
            _stableText = null;
            _textStillMs = 0;
            return;
        }

        var raw = UiAutomationTextCapture.GetTextAtScreenPoint(pos.X, pos.Y);
        var text = TextHeuristics.ResolveHoverTarget(raw);

        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            _stableText = null;
            _textStillMs = 0;
            return;
        }

        if (!string.Equals(text, _stableText, StringComparison.OrdinalIgnoreCase))
        {
            _stableText = text;
            _textStillMs = 0;
            return;
        }

        _textStillMs += 150;
        var debounceMs = HoverProtectionOptions.FromConfig(config).EffectiveDebounceMs(config);
        if (_textStillMs < debounceMs) return;

        _busy = true;
        _ = RunHoverTranslateAsync(text);
    }

    private async Task RunHoverTranslateAsync(string text)
    {
        try
        {
            await _coordinator.TranslateTextAsync(text, fromHover: true).ConfigureAwait(false);
        }
        finally
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _busy = false;
                _textStillMs = 0;
            });
        }
    }

    private static bool IsLeftButtonDown() =>
        (GetAsyncKeyState(0x01) & 0x8000) != 0;

    private static (int X, int Y) GetCursorPos()
    {
        if (GetCursorPos(out var p)) return (p.X, p.Y);
        return (-1, -1);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public void Dispose() => _timer.Stop();
}
