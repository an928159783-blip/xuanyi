namespace HoverTranslate.Core.Services;

public static class HotkeyParser
{
    public const string DefaultHotkey = "Ctrl+Shift+T";

    public static (uint Modifiers, uint VirtualKey) Parse(string? hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
            return Parse(DefaultHotkey);

        uint mods = 0;
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string? keyPart = null;

        foreach (var part in parts)
        {
            if (TryParseModifier(part, out var mod))
                mods |= mod;
            else
                keyPart = part;
        }

        if (keyPart is null)
            throw new FormatException($"热键格式无效：{hotkey}");

        return (mods, KeyNameToVirtualKey(keyPart));
    }

    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0008) != 0) parts.Add("Win");
        parts.Add(VirtualKeyToDisplayName(virtualKey));
        return string.Join("+", parts);
    }

    private static bool TryParseModifier(string part, out uint mod)
    {
        mod = part.ToLowerInvariant() switch
        {
            "ctrl" or "control" => 0x0002,
            "shift" => 0x0004,
            "alt" => 0x0001,
            "win" or "windows" => 0x0008,
            _ => 0
        };
        return mod != 0;
    }

    private static uint KeyNameToVirtualKey(string name)
    {
        if (name.Length == 1)
        {
            var c = char.ToUpperInvariant(name[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return c;
        }

        if (name.StartsWith("F", StringComparison.OrdinalIgnoreCase)
            && name.Length > 1
            && int.TryParse(name[1..], out var fn) && fn is >= 1 and <= 24)
            return (uint)(0x70 + fn - 1);

        throw new FormatException($"不支持的按键：{name}");
    }

    private static string VirtualKeyToDisplayName(uint vk) => vk switch
    {
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x70 and <= 0x87 => $"F{vk - 0x70 + 1}",
        0x20 => "Space",
        _ => $"VK{vk:X}"
    };
}
