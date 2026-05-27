using System.Diagnostics;
using System.IO;
namespace HoverTranslate.App.Services;

public static class LegalDocuments
{
    public static string DocsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "docs");

    public static void Open(string fileName)
    {
        var path = Path.Combine(DocsDirectory, fileName);
        if (!File.Exists(path))
        {
            System.Windows.MessageBox.Show(
                $"未找到文档：{path}\n请确认已使用 publish 目录中的完整安装包。",
                AppBranding.SettingsTitle,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"无法打开文档：{ex.Message}\n{path}",
                AppBranding.SettingsTitle,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
