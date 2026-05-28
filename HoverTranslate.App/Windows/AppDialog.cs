using System.Windows;
using System.Windows.Threading;

namespace HoverTranslate.App.Windows;

/// <summary>与设置页一致的 Frost 风格对话框，替代系统 MessageBox。</summary>
public static class AppDialog
{
    public enum ImportHistoryMode
    {
        Merge,
        Replace
    }

    public static MessageBoxResult Show(
        string message,
        string? title = null,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        Window? owner = null,
        bool dangerPrimary = false)
    {
        var result = ShowCore(message, title, button, icon, owner, dangerPrimary, AppDialogKind.Default,
                modal: owner is null)
            .GetAwaiter().GetResult();
        return result ?? MessageBoxResult.None;
    }

    /// <summary>有 owner 时用非模态 Show，设置窗等可在弹框打开时拖动。</summary>
    public static bool Confirm(
        string message,
        string? title = null,
        Window? owner = null,
        MessageBoxImage icon = MessageBoxImage.Question,
        bool dangerPrimary = false) =>
        Show(message, title, MessageBoxButton.YesNo, icon, owner, dangerPrimary) == MessageBoxResult.Yes;

    public static Task<MessageBoxResult?> ShowAsync(
        string message,
        string? title = null,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        Window? owner = null,
        bool dangerPrimary = false) =>
        ShowCore(message, title, button, icon, owner, dangerPrimary, AppDialogKind.Default, modal: false);

    public static async Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        Window? owner = null,
        MessageBoxImage icon = MessageBoxImage.Question,
        bool dangerPrimary = false) =>
        await ShowAsync(message, title, MessageBoxButton.YesNo, icon, owner, dangerPrimary).ConfigureAwait(true)
        == MessageBoxResult.Yes;

    public static async Task<ImportHistoryMode?> AskImportHistoryModeAsync(
        string? title = null,
        Window? owner = null)
    {
        var result = await ShowCore(
            "请选择导入方式：\n\n「追加」：保留现有记录并写入导入内容\n「清空后导入」：先删除本机全部历史，再写入导入内容\n「取消」：不导入",
            title ?? AppBranding.SettingsTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Question,
            owner,
            dangerPrimary: false,
            AppDialogKind.ImportHistory,
            modal: false).ConfigureAwait(true);

        return result switch
        {
            MessageBoxResult.Yes => ImportHistoryMode.Merge,
            MessageBoxResult.No => ImportHistoryMode.Replace,
            _ => null
        };
    }

    public static void Info(string message, string? title = null, Window? owner = null)
    {
        if (owner is { IsLoaded: true })
            _ = ShowAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Information, owner);
        else
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information, owner);
    }

    public static void Warning(string message, string? title = null, Window? owner = null)
    {
        if (owner is { IsLoaded: true })
            _ = ShowAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Warning, owner);
        else
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning, owner);
    }

    public static void Error(string message, string? title = null, Window? owner = null)
    {
        if (owner is { IsLoaded: true })
            _ = ShowAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Error, owner);
        else
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, owner);
    }

    private static Task<MessageBoxResult?> ShowCore(
        string message,
        string? title,
        MessageBoxButton button,
        MessageBoxImage icon,
        Window? owner,
        bool dangerPrimary,
        AppDialogKind kind,
        bool modal)
    {
        var tcs = new TaskCompletionSource<MessageBoxResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dlg = new AppDialogWindow(message, title ?? AppBranding.SettingsTitle, button, icon, dangerPrimary, kind);

        if (owner is { IsLoaded: true })
        {
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dlg.Closed += (_, _) =>
        {
            var result = dlg.Result == MessageBoxResult.None ? (MessageBoxResult?)null : dlg.Result;
            tcs.TrySetResult(result);
        };

        if (modal)
        {
            dlg.ShowDialog();
            return Task.FromResult<MessageBoxResult?>(dlg.Result == MessageBoxResult.None ? null : dlg.Result);
        }

        dlg.Show();
        dlg.Activate();

        if (SynchronizationContext.Current is DispatcherSynchronizationContext)
        {
            var frame = new DispatcherFrame();
            dlg.Closed += (_, _) => frame.Continue = false;
            Dispatcher.PushFrame(frame);
            var syncResult = dlg.Result == MessageBoxResult.None ? (MessageBoxResult?)null : dlg.Result;
            return Task.FromResult(syncResult);
        }

        return tcs.Task;
    }
}
