namespace HoverTranslate.App;

public static class AppBranding
{
    public const string DisplayName = "炫译";
    public const string SettingsTitle = "炫译 设置";

    /// <summary>发布时改为真实 Releases 页；留空 API 则仅打开此 URL。</summary>
    public const string ReleasesPageUrl = "https://github.com/REPLACE_OWNER/hover-translate/releases";

    /// <summary>配置 GitHub API 后可在线比对版本；未配置仓库时留空。</summary>
    public const string GitHubLatestReleaseApiUrl = "";

    public static string FormatHotkeyTip(string hotkey) =>
        $"拖选英文 → 按 {hotkey}；或在设置中开启「复制后自动翻译」后只按 Ctrl+C";
}
