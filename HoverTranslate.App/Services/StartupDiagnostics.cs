using System.IO;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public static class StartupDiagnostics
{
    private const long MaxLogBytes = 1024 * 1024;
    private const int MaxArchivedLogs = 3;

    public static string LogPath => Path.Combine(ConfigService.ConfigDirectory, "startup.log");

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(ConfigService.ConfigDirectory);
            RotateIfNeeded();
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }

    public static void LogException(string stage, Exception ex) =>
        Log($"{stage}: {ex}");

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
            return;

        var info = new FileInfo(LogPath);
        if (info.Length < MaxLogBytes)
            return;

        var dir = Path.GetDirectoryName(LogPath)!;
        var oldest = Path.Combine(dir, $"startup.log.{MaxArchivedLogs}");
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (var i = MaxArchivedLogs - 1; i >= 1; i--)
        {
            var from = Path.Combine(dir, i == 1 ? "startup.log" : $"startup.log.{i - 1}");
            var to = Path.Combine(dir, $"startup.log.{i}");
            if (File.Exists(from))
            {
                if (File.Exists(to))
                    File.Delete(to);
                File.Move(from, to);
            }
        }
    }
}
