using System.Drawing;
using System.IO;
using System.Reflection;

namespace HoverTranslate.App.Services;

public static class AppIconHelper
{
    private static Icon? _cached;

    public static Icon LoadTrayIcon()
    {
        if (_cached is not null)
            return _cached;

        var baseDir = AppContext.BaseDirectory;
        foreach (var name in new[] { "xuanyi.ico", "Assets\\xuanyi.ico" })
        {
            var path = Path.Combine(baseDir, name);
            if (!File.Exists(path)) continue;
            try
            {
                _cached = new Icon(path);
                return _cached;
            }
            catch
            {
                // 损坏的 ico 不应阻止程序启动
            }
        }

        _cached = SystemIcons.Application;
        return _cached;
    }
}
