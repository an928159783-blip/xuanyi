namespace HoverTranslate.App;

public static class AppBranding
{
    public const string DisplayName = "炫译";
    public const string SettingsTitle = "炫译 设置";

    public const string ReleasesPageUrl = "https://github.com/an928159783-blip/xuanyi/releases";

    public const string GitHubLatestReleaseApiUrl =
        "https://api.github.com/repos/an928159783-blip/xuanyi/releases/latest";

    public static string FormatHotkeyTip(string hotkey) =>
        $"拖选英文 → 按 {hotkey}；或在设置中开启「复制后自动翻译」后只按 Ctrl+C";
}
