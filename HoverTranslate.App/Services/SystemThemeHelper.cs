using Microsoft.Win32;

namespace HoverTranslate.App.Services;

internal static class SystemThemeHelper
{
    public static bool IsAppsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
        }
        catch
        {
            // ignore
        }

        return false;
    }
}
