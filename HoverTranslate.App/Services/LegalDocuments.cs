using System.Diagnostics;
using System.IO;
using HoverTranslate.App.Windows;

namespace HoverTranslate.App.Services;

public static class LegalDocuments
{
    public static string DocsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "docs");

    public static void Open(string fileName, System.Windows.Window? owner = null)
    {
        var path = Path.Combine(DocsDirectory, fileName);
        if (!File.Exists(path))
        {
            AppDialog.Warning(
                $"未找到文档：{path}\n请确认已使用 publish 目录中的完整安装包。",
                AppBranding.SettingsTitle,
                owner);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"无法打开文档：{ex.Message}\n{path}", AppBranding.SettingsTitle, owner);
        }
    }
}
