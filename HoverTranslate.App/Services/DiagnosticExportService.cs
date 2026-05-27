using System.IO;
using System.IO.Compression;
using System.Reflection;
using HoverTranslate.Core.Services;
namespace HoverTranslate.App.Services;

public static class DiagnosticExportService
{
    public static bool ExportWithPrompt()
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出诊断包",
            Filter = "ZIP 压缩包|*.zip",
            FileName = $"xuanyi-diagnostics-{stamp}.zip"
        };

        if (dialog.ShowDialog() != true)
            return false;

        try
        {
            ExportToZip(dialog.FileName);
            System.Windows.MessageBox.Show($"已导出：{dialog.FileName}", AppBranding.DisplayName,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"导出失败：{ex.Message}", AppBranding.DisplayName,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }
    }

    public static void ExportToZip(string zipPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "xuanyi-diag-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            File.WriteAllText(Path.Combine(tempDir, "version.txt"),
                $"Product: {AppBranding.DisplayName}\r\nVersion: {version}\r\nExported: {DateTime.Now:O}\r\n");

            File.WriteAllText(Path.Combine(tempDir, "config.redacted.json"), ConfigRedaction.ReadRedactedConfigFile());

            CopyLogFiles(tempDir);

            if (File.Exists(HistoryStore.DatabasePath))
            {
                File.Copy(HistoryStore.DatabasePath, Path.Combine(tempDir, "history.db"), true);
            }

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempDir, zipPath);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    private static void CopyLogFiles(string destDir)
    {
        var dir = ConfigService.ConfigDirectory;
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.GetFiles(dir, "startup.log*"))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destDir, name), true);
        }
    }
}
