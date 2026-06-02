using System.Windows;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

public static class SettingsWindowHost
{
    private static SettingsWindow? _instance;
    private static Action<AppConfig>? _onSaved;

    public static bool IsOpen => _instance is { IsVisible: true };

    /// <summary>注册保存后回调（热键、悬停、托盘等）；应用启动时调用一次即可。</summary>
    public static void RegisterSavedHandler(Action<AppConfig> onSaved) => _onSaved = onSaved;

    public static void ShowOrActivate(Action<AppConfig>? onSaved = null, int? navIndex = null)
    {
        if (onSaved is not null)
            _onSaved = onSaved;

        if (_instance is null)
        {
            _instance = new SettingsWindow();
            _instance.Saved += (_, cfg) => _onSaved?.Invoke(cfg);
            _instance.Closed += (_, _) => _instance = null;
        }

        _instance.Show();
        _instance.Activate();
        _instance.Focus();
        if (navIndex is >= 0)
            _instance.SelectNav(navIndex.Value);
    }

    public static void ShowFeatureGuide(Window? owner = null)
    {
        var guide = new FeatureGuideWindow();
        if (owner is { IsVisible: true })
            guide.Owner = owner;
        guide.ShowDialog();
    }
}
