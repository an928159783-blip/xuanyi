namespace HoverTranslate.App;

public static class AppBranding
{
    public const string DisplayName = "炫译";
    public const string SettingsTitle = "炫译 设置";

    public static string FormatHotkeyTip(string hotkey) =>
        $"拖选英文 → 按 {hotkey}；或在设置中开启「复制后自动翻译」后只按 Ctrl+C";
}
