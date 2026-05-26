using System.IO;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public static class StartupDiagnostics
{
    public static string LogPath => Path.Combine(ConfigService.ConfigDirectory, "startup.log");

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(ConfigService.ConfigDirectory);
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
}
