using System.Windows;

namespace HoverTranslate.App.Windows;

/// <summary>与设置页一致的 Frost 风格对话框，替代系统 MessageBox。</summary>
public static class AppDialog
{
    public static MessageBoxResult Show(
        string message,
        string? title = null,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        Window? owner = null,
        bool dangerPrimary = false)
    {
        var dlg = new AppDialogWindow(message, title ?? AppBranding.SettingsTitle, button, icon, dangerPrimary);
        if (owner is { IsLoaded: true })
        {
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dlg.ShowDialog();
        return dlg.Result;
    }

    public static bool Confirm(
        string message,
        string? title = null,
        Window? owner = null,
        MessageBoxImage icon = MessageBoxImage.Question,
        bool dangerPrimary = false) =>
        Show(message, title, MessageBoxButton.YesNo, icon, owner, dangerPrimary) == MessageBoxResult.Yes;

    public static void Info(string message, string? title = null, Window? owner = null) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information, owner);

    public static void Warning(string message, string? title = null, Window? owner = null) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning, owner);

    public static void Error(string message, string? title = null, Window? owner = null) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, owner);
}
