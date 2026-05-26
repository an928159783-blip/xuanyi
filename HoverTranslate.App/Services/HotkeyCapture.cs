using System.Windows.Input;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public static class HotkeyCapture
{
    public static bool TryFormat(ModifierKeys modifiers, Key key, out string hotkey)
    {
        hotkey = "";
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System or Key.None)
            return false;

        uint mods = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mods |= 0x0008;

        if (mods == 0)
            return false;

        try
        {
            var vk = KeyToVirtualKey(key);
            hotkey = HotkeyParser.Format(mods, vk);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static uint KeyToVirtualKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => (uint)('A' + (key - Key.A)),
        >= Key.D0 and <= Key.D9 => (uint)('0' + (key - Key.D0)),
        >= Key.NumPad0 and <= Key.NumPad9 => (uint)('0' + (key - Key.NumPad0)),
        >= Key.F1 and <= Key.F24 => (uint)(0x70 + (key - Key.F1)),
        Key.Space => 0x20,
        _ => throw new FormatException($"不支持的按键：{key}")
    };
}
