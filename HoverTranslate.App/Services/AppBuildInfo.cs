using System.IO;

namespace HoverTranslate.App.Services;

public static class AppBuildInfo
{
    public static string? ReadBuildStamp()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "build-stamp.txt");
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    public static string FormatForDisplay()
    {
        var stamp = ReadBuildStamp();
        return stamp is null ? "构建时间未知（请用 publish 目录启动）" : $"构建 {stamp}";
    }
}
