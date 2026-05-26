using System.Windows;
using System.Windows.Threading;

namespace HoverTranslate.App.Services;

internal static class UiDispatcher
{
    public static async Task RunAsync(Func<Task> action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF 应用未启动。");

        if (dispatcher.CheckAccess())
        {
            await action().ConfigureAwait(true);
            return;
        }

        await dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task.ConfigureAwait(true);
    }
}
