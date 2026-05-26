using System.Windows.Interop;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class HotkeyService : IDisposable
{
    private HwndSource? _hwndSource;
    private bool _registered;
    private string _currentHotkey = HotkeyParser.DefaultHotkey;

    public event EventHandler? HotkeyPressed;

    public string CurrentHotkey => _currentHotkey;

    public void Register(string? hotkey = null) => ApplyHotkey(hotkey ?? HotkeyParser.DefaultHotkey);

    public void ApplyHotkey(string hotkey)
    {
        EnsureHost();
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_hwndSource!.Handle, NativeMethods.HotkeyId);
            _registered = false;
        }

        var (mods, vk) = HotkeyParser.Parse(hotkey);
        if (!NativeMethods.RegisterHotKey(_hwndSource!.Handle, NativeMethods.HotkeyId, mods, vk))
            throw new InvalidOperationException($"无法注册全局热键「{hotkey}」，可能已被其它程序占用。");

        _currentHotkey = hotkey;
        _registered = true;
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
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == NativeMethods.HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registered && _hwndSource is not null)
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, NativeMethods.HotkeyId);
        _registered = false;
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
