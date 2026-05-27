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
    private readonly Action _onTranslate;
    private readonly Action _onExit;
    private readonly Action _onShowHistory;
    private readonly Action _onShowTranslation;
    private readonly Action _onOpenSettings;

    public TrayService(
        ConfigService configService,
        AppConfig config,
        Action onTranslate,
        Action onExit,
        Action onShowHistory,
        Action onShowTranslation,
        Action onOpenSettings,
        Action<bool> onHoverSettingChanged)
    {
        _configService = configService;
        _config = config;
        _onTranslate = onTranslate;
        _onExit = onExit;
        _onShowHistory = onShowHistory;
        _onShowTranslation = onShowTranslation;
        _onOpenSettings = onOpenSettings;
        _ = onHoverSettingChanged;

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

        var hk = string.IsNullOrWhiteSpace(_config.Hotkey) ? HotkeyParser.DefaultHotkey : _config.Hotkey;

        var translate = new ToolStripMenuItem($"翻译选中  {hk}")
        {
            TextAlign = ContentAlignment.MiddleCenter,
            ToolTipText = "先拖选一段英文，再按此热键（会自动读取选中，不必先复制）"
        };
        translate.Click += (_, _) => _onTranslate();
        menu.Items.Add(translate);

        var translationWin = new ToolStripMenuItem("打开译文窗");
        translationWin.Click += (_, _) => _onShowTranslation();
        menu.Items.Add(translationWin);

        var historyWin = new ToolStripMenuItem("打开历史窗");
        historyWin.Click += (_, _) => _onShowHistory();
        menu.Items.Add(historyWin);

        var historyItem = new ToolStripMenuItem(_config.EnableHistory ? "历史写入：开" : "历史写入：关")
        {
            ToolTipText = "点击切换是否保存翻译历史"
        };
        historyItem.Click += (_, _) => ToggleHistorySave();
        menu.Items.Add(historyItem);

        menu.Items.Add(new ToolStripSeparator());

        var settings = new ToolStripMenuItem("设置") { TextAlign = ContentAlignment.MiddleCenter };
        settings.Click += (_, _) => _onOpenSettings();
        menu.Items.Add(settings);

        var checkUpdate = new ToolStripMenuItem("检查更新")
        {
            ToolTipText = $"当前版本 {UpdateCheckService.CurrentVersion}"
        };
        checkUpdate.Click += (_, _) => _ = UpdateCheckService.CheckAndNotifyAsync(null);
        menu.Items.Add(checkUpdate);

        var exportDiag = new ToolStripMenuItem("导出诊断包")
        {
            ToolTipText = "导出日志与脱敏配置（不含 API Key）"
        };
        exportDiag.Click += (_, _) => DiagnosticExportService.ExportWithPrompt();
        menu.Items.Add(exportDiag);

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("退出炫译");
        exit.Click += (_, _) => _onExit();
        menu.Items.Add(exit);

        _icon.ContextMenuStrip = menu;
    }

    public void ShowBalloon(string title, string message) =>
        _icon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);

    private void ToggleHistorySave()
    {
        _configService.SaveMerged(c => c.EnableHistory = !c.EnableHistory);
        RefreshConfig();
        ShowBalloon(AppBranding.DisplayName,
            _config.EnableHistory ? "已开启：翻译将写入历史" : "已关闭：不再保存历史");
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
