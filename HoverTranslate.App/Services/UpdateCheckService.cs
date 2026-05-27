using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using HoverTranslate.App.Windows;

namespace HoverTranslate.App.Services;

public static class UpdateCheckService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    static UpdateCheckService()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "HoverTranslate-UpdateCheck/1.0");
    }

    public static string CurrentVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v is null ? "0.1.0" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public static async Task CheckAndNotifyAsync(Window? owner)
    {
        var latest = await TryGetLatestReleaseTagAsync().ConfigureAwait(true);
        if (latest is null)
        {
            if (AppDialog.Confirm(
                    $"当前版本：{CurrentVersion}\n\n无法在线检查更新（未配置仓库或网络不可用）。\n是否打开发行说明页面？",
                    AppBranding.DisplayName,
                    owner,
                    MessageBoxImage.Information))
                OpenReleasesPage(owner);
            return;
        }

        var tag = latest.TrimStart('v', 'V');
        if (IsNewerVersion(tag, CurrentVersion))
        {
            if (AppDialog.Confirm(
                    $"发现新版本：{latest}\n当前版本：{CurrentVersion}\n\n是否打开下载页面？",
                    AppBranding.DisplayName,
                    owner,
                    MessageBoxImage.Information))
                OpenReleasesPage(owner);
        }
        else
        {
            AppDialog.Info($"当前已是最新版本（{CurrentVersion}）。", AppBranding.DisplayName, owner);
        }
    }

    public static void OpenReleasesPage(Window? owner = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(AppBranding.ReleasesPageUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"无法打开浏览器：{ex.Message}\n{AppBranding.ReleasesPageUrl}", AppBranding.DisplayName, owner);
        }
    }

    private static async Task<string?> TryGetLatestReleaseTagAsync()
    {
        var api = AppBranding.GitHubLatestReleaseApiUrl;
        if (string.IsNullOrWhiteSpace(api))
            return null;

        try
        {
            await using var stream = await Http.GetStreamAsync(api).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                return tag.GetString();
        }
        catch
        {
            // fall through
        }

        return null;
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (!Version.TryParse(latest, out var lv) || !Version.TryParse(current, out var cv))
            return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
        return lv > cv;
    }
}
