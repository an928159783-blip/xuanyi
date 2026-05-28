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

    private string? _stableSelection;
    private int _selectionStillMs;
    private string? _lastSelectionTranslated;

    private string? _lastHoverTranslated;

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
        if (!_configService.Load().EnableHover && !_configService.Load().TranslateOnSelection) return;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void PauseFor(int milliseconds) =>
        _pausedUntil = DateTime.UtcNow.AddMilliseconds(milliseconds);

    public void OnManualTranslateStarting()
    {
        _pausedUntil = DateTime.UtcNow.AddSeconds(15);
        _busy = true;
        _stableText = null;
        _textStillMs = 0;
        _stableSelection = null;
        _selectionStillMs = 0;
        _lastX = -1;
        _lastY = -1;
    }

    public void OnManualTranslateFinished() => _busy = false;

    private void OnTick(object? sender, EventArgs e)
    {
        if (_busy || DateTime.UtcNow < _pausedUntil) return;

        var config = _configService.Load();
        if (IsLeftButtonDown()) return;

        // 仅当光标在炫译自有浮窗上时暂停；勿因译文/历史窗「已打开」就全局禁用悬停。
        if (AppWindowHoverGuard.ShouldSuppressHoverCapture())
        {
            _stableSelection = null;
            _selectionStillMs = 0;
            _stableText = null;
            _textStillMs = 0;
            _lastHoverTranslated = null;
            return;
        }

        if (config.TranslateOnSelection && TryTickSelectionTranslate(config))
            return;

        if (!config.EnableHover) return;

        if (UiAutomationSelectionCapture.HasActiveSelection())
            return;

        TickHoverTranslate(config);
    }

    private bool TryTickSelectionTranslate(AppConfig config)
    {
        var selection = UiAutomationSelectionCapture.TryGetSelectedText()?.Trim();
        if (!TranslationDirectionResolver.ShouldTranslateText(selection ?? "", config))
        {
            _stableSelection = null;
            _selectionStillMs = 0;
            return false;
        }

        selection = selection!.Trim();
        if (!string.Equals(selection, _stableSelection, StringComparison.Ordinal))
        {
            _stableSelection = selection;
            _selectionStillMs = 0;
            return true;
        }

        _selectionStillMs += 150;
        var debounceMs = Math.Max(350, HoverProtectionOptions.FromConfig(config).EffectiveDebounceMs(config) / 2);
        if (_selectionStillMs < debounceMs) return true;

        if (string.Equals(selection, _lastSelectionTranslated, StringComparison.Ordinal))
            return true;

        _lastSelectionTranslated = selection;
        _busy = true;
        _ = RunSelectionTranslateAsync(selection);
        return true;
    }

    private void TickHoverTranslate(AppConfig config)
    {
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
        var text = TextHeuristics.ResolveHoverTarget(raw, config.TranslationDirection);

        if (string.IsNullOrWhiteSpace(text) || text.Length < 2
            || !TranslationDirectionResolver.ShouldTranslateText(text, config))
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

        if (string.Equals(text, _lastHoverTranslated, StringComparison.Ordinal))
            return;

        if (text.Length > config.MaxCharsPerRequest)
            return;

        _lastHoverTranslated = text;
        _busy = true;
        _ = RunHoverTranslateAsync(text);
    }

    private async Task RunSelectionTranslateAsync(string text)
    {
        try
        {
            await _coordinator.TranslateTextAsync(text, fromHover: false, forceShowTranslation: true)
                .ConfigureAwait(false);
        }
        finally
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _busy = false;
                _selectionStillMs = 0;
            });
        }
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
