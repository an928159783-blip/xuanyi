using System.Windows.Interop;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class HotkeyService : IDisposable
{
    private HwndSource? _hwndSource;
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly Dictionary<int, string> _registeredHotkeys = new();
    private string _currentHotkey = HotkeyParser.DefaultHotkey;

    public event EventHandler? HotkeyPressed;
    public event EventHandler? ScreenshotHotkeyPressed;

    public string CurrentHotkey => _currentHotkey;

    public void Register(string? hotkey = null) => ApplyConfig(new AppConfig { Hotkey = hotkey ?? HotkeyParser.DefaultHotkey });

    public void ApplyConfig(AppConfig config)
    {
        EnsureHost();
        UnregisterAll();

        _currentHotkey = string.IsNullOrWhiteSpace(config.Hotkey)
            ? HotkeyParser.DefaultHotkey
            : config.Hotkey.Trim();

        RegisterOne(NativeMethods.HotkeyIdTranslate, _currentHotkey, () => HotkeyPressed?.Invoke(this, EventArgs.Empty));

        if (config.EnableScreenshotRegionHotkey)
        {
            var shot = string.IsNullOrWhiteSpace(config.ScreenshotRegionHotkey)
                ? "Ctrl+Shift+S"
                : config.ScreenshotRegionHotkey.Trim();
            RegisterOne(NativeMethods.HotkeyIdScreenshot, shot, () => ScreenshotHotkeyPressed?.Invoke(this, EventArgs.Empty));
        }
    }

    public void ApplyHotkey(string hotkey) => ApplyConfig(new AppConfig { Hotkey = hotkey });

    private void RegisterOne(int id, string hotkey, Action handler)
    {
        var (mods, vk) = HotkeyParser.Parse(hotkey);
        if (!NativeMethods.RegisterHotKey(_hwndSource!.Handle, id, mods, vk))
            throw new InvalidOperationException($"无法注册全局热键「{hotkey}」，可能已被其它程序占用。");

        _handlers[id] = handler;
        _registeredHotkeys[id] = hotkey;
    }

    private void UnregisterAll()
    {
        if (_hwndSource is null) return;
        foreach (var id in _registeredHotkeys.Keys.ToList())
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, id);
        _handlers.Clear();
        _registeredHotkeys.Clear();
    }

    private void EnsureHost()
    {
        if (_hwndSource is not null) return;

        var parameters = new HwndSourceParameters("XuanYiHotkeyHost")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0x08000000 | 0x00000080
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_handlers.TryGetValue(id, out var action))
            {
                action();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
