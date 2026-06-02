using System.Drawing;
using System.Windows.Forms;
using HoverTranslate.App;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ConfigService _configService;
    private AppConfig _config;
    private readonly Action _onExit;
    private readonly Action _onShowHistory;
    private readonly Action _onShowTranslation;
    private readonly Action _onOpenSettings;
    private readonly Action _onScreenshotRegion;

    public TrayService(
        ConfigService configService,
        AppConfig config,
        Action onExit,
        Action onShowHistory,
        Action onShowTranslation,
        Action onOpenSettings,
        Action onScreenshotRegion)
    {
        _configService = configService;
        _config = config;
        _onExit = onExit;
        _onShowHistory = onShowHistory;
        _onShowTranslation = onShowTranslation;
        _onOpenSettings = onOpenSettings;
        _onScreenshotRegion = onScreenshotRegion;

        try
        {
            _icon = new NotifyIcon
            {
                Icon = AppIconHelper.LoadTrayIcon(),
                Visible = true,
                Text = AppBranding.DisplayName
            };
            RebuildMenu();
            _icon.DoubleClick += (_, _) => _onShowTranslation();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("TrayService", ex);
            throw;
        }
    }

    public void RefreshConfig()
    {
        _config = _configService.Load();
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9.75f),
            Padding = new Padding(6, 8, 6, 8),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Renderer = new RoundedTrayMenuRenderer()
        };
        menu.Opening += (_, _) =>
        {
            RoundedTrayMenuRenderer.ApplyRoundedRegion(menu);
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem mi)
                    mi.TextAlign = ContentAlignment.MiddleCenter;
            }
        };

        var translationWin = new ToolStripMenuItem("打开译文窗")
        {
            TextAlign = ContentAlignment.MiddleCenter,
            ToolTipText = "手动打开译文窗；翻译热键、历史与检查更新等请在「设置」中配置"
        };
        translationWin.Click += (_, _) => _onShowTranslation();
        menu.Items.Add(translationWin);

        var historyWin = new ToolStripMenuItem("打开历史窗");
        historyWin.Click += (_, _) => _onShowHistory();
        menu.Items.Add(historyWin);

        var screenshot = new ToolStripMenuItem("框选截屏翻译")
        {
            ToolTipText = "拖选屏幕区域 OCR 后翻译；热键可在设置 → 热键与悬停中配置"
        };
        screenshot.Click += (_, _) => _onScreenshotRegion();
        menu.Items.Add(screenshot);

        menu.Items.Add(new ToolStripSeparator());

        var settings = new ToolStripMenuItem("设置") { TextAlign = ContentAlignment.MiddleCenter };
        settings.Click += (_, _) => _onOpenSettings();
        menu.Items.Add(settings);

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("退出炫译");
        exit.Click += (_, _) => _onExit();
        menu.Items.Add(exit);

        _icon.ContextMenuStrip = menu;
    }

    public void ShowBalloon(string title, string message) =>
        _icon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
