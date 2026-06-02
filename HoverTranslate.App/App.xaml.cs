using System.Windows;
using System.Windows.Threading;
using HoverTranslate.App.Services;
using HoverTranslate.App.Windows;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "Global\\XuanYi.HoverTranslate.SingleInstance";
    private static Mutex? _singleInstanceMutex;

    private TrayService? _tray;
    private HotkeyService? _hotkey;
    private ClipboardTranslateService? _clipboardTranslate;
    private TranslationCoordinator? _coordinator;
    private HoverTranslateService? _hover;
    private readonly SelectionCaptureService _selectionCapture = new();
    private ConfigService? _configService;
    private ScreenshotTranslateService? _screenshotTranslate;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryAcquireSingleInstance())
        {
            AppDialog.Info(
                $"炫译已在运行（托盘里仍是旧进程，不会自动换成新 exe）。\n\n" +
                $"请先托盘「退出炫译」，再双击桌面快捷方式。\n\n" +
                $"本次点击的启动包：{AppBuildInfo.FormatForDisplay()}\n" +
                $"路径：{AppContext.BaseDirectory}",
                AppBranding.DisplayName);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            AppDialog.Error(args.Exception.ToString(), $"{AppBranding.DisplayName} 错误");
            args.Handled = true;
        };

        try
        {
            StartupDiagnostics.Log("OnStartup begin");
            _configService = new ConfigService();
            _configService.EnsureConfigExists();
            var config = _configService.Load();
            SettingsWindowHost.RegisterSavedHandler(OnSettingsSaved);

            _coordinator = new TranslationCoordinator(_configService);
            _hover = new HoverTranslateService(_coordinator, _configService);
            _coordinator.AttachHover(_hover);
            _screenshotTranslate = new ScreenshotTranslateService(_coordinator, _configService);

            _tray = new TrayService(
                _configService,
                config,
                OnExit,
                OnShowHistory,
                OnShowTranslation,
                OnOpenSettings,
                OnScreenshotRegionTranslate);

            _hotkey = new HotkeyService();
            _hotkey.HotkeyPressed += (_, _) => OnTranslateRequested();
            _hotkey.ScreenshotHotkeyPressed += (_, _) => OnScreenshotRegionTranslate();
            try
            {
                _hotkey.ApplyConfig(config);
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogException("Hotkey", ex);
                AppDialog.Warning($"{ex.Message}\n将使用默认热键。", AppBranding.DisplayName);
                _hotkey.ApplyConfig(new Core.Models.AppConfig());
            }

            ApplyClipboardTranslate(config);
            ApplyHoverFromSettings(config.EnableHover || config.TranslateOnSelection);
            HoverGuard.RegisterPauseHandler(ms => _hover?.PauseFor(ms));

            var hk = _hotkey.CurrentHotkey;
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (config.ShowStartupNotice)
                    {
                        var notice = new StartupNoticeWindow();
                        notice.Closed += (_, _) =>
                        {
                            if (notice.DontShowAgain)
                                _configService.SaveMerged(c => c.ShowStartupNotice = false);
                            if (!ConfigService.HasApiKey(_configService.Load()))
                                SettingsWindowHost.ShowOrActivate();
                        };
                        notice.Show();
                    }
                    else if (!ConfigService.HasApiKey(_configService.Load()))
                    {
                        SettingsWindowHost.ShowOrActivate();
                    }
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.LogException("PostStartupUi", ex);
                    AppDialog.Error(ex.ToString(), $"{AppBranding.DisplayName} 启动界面失败");
                }
            }, DispatcherPriority.ApplicationIdle);

            var shotHk = config.EnableScreenshotRegionHotkey
                ? (string.IsNullOrWhiteSpace(config.ScreenshotRegionHotkey) ? "Ctrl+Shift+S" : config.ScreenshotRegionHotkey.Trim())
                : null;
            var shotHint = shotHk is null
                ? "托盘可「框选截屏翻译」。"
                : $"{shotHk} 框选截屏翻译；";
            _tray.ShowBalloon(AppBranding.DisplayName,
                config.TranslateOnCopy
                    ? $"已启动。{hk} 翻译选中；{shotHint}复制后自动翻译已开启。"
                    : $"已启动。{hk} 翻译选中；{shotHint}");
            StartupDiagnostics.Log("OnStartup ok");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("OnStartup", ex);
            AppDialog.Error(
                $"启动失败：{ex.Message}\n\n详情已写入：\n{StartupDiagnostics.LogPath}\n\n{ex}",
                AppBranding.DisplayName);
            Shutdown();
        }
    }

    private void OnTranslateRequested(string? knownText = null)
    {
        if (_coordinator is null) return;

        if (string.IsNullOrWhiteSpace(knownText))
            _clipboardTranslate?.PauseFor(1500);

        Current.Dispatcher.BeginInvoke(() =>
        {
            string? captured = knownText?.Trim();
            if (string.IsNullOrWhiteSpace(captured))
            {
                try
                {
                    captured = _selectionCapture.GetTextForTranslation();
                }
                catch
                {
                    // ignore
                }
            }

            _ = _coordinator.TranslateFromSelectionOrClipboardAsync(
                captured,
                fromHotkey: string.IsNullOrWhiteSpace(knownText));
        }, DispatcherPriority.Normal);
    }

    private void ApplyHoverFromSettings(bool enableHover)
    {
        var config = _configService?.Load();
        if (enableHover || config?.TranslateOnSelection == true)
            _hover?.Start();
        else
            _hover?.Stop();
    }

    private void ApplyClipboardTranslate(Core.Models.AppConfig config)
    {
        _clipboardTranslate?.Dispose();
        _clipboardTranslate = null;

        if (!config.TranslateOnCopy || _configService is null)
        {
            ClipboardGuard.RegisterPauseHandler(_ => { });
            return;
        }

        _clipboardTranslate = new ClipboardTranslateService();
        _clipboardTranslate.Start(_configService, text => OnTranslateRequested(text));
        ClipboardGuard.RegisterPauseHandler(ms => _clipboardTranslate.PauseFor(ms));
    }

    public Task TriggerScreenshotRegionTranslateAsync() =>
        _screenshotTranslate?.StartRegionPickAndTranslateAsync() ?? Task.CompletedTask;

    private void OnScreenshotRegionTranslate() => _ = TriggerScreenshotRegionTranslateAsync();

    private void OnOpenSettings() => Current.Dispatcher.Invoke(() =>
    {
        try
        {
            SettingsWindowHost.ShowOrActivate(OnSettingsSaved);
        }
        catch (Exception ex)
        {
            ShowUiError("打开设置失败", ex);
        }
    });

    private void OnSettingsSaved(AppConfig saved)
    {
        ApplyHoverFromSettings(saved.EnableHover || saved.TranslateOnSelection);

        try
        {
            _hotkey?.ApplyConfig(saved);
        }
        catch (Exception ex)
        {
            ShowUiError("热键注册失败", ex);
        }

        ApplyClipboardTranslate(saved);
        _tray?.RefreshConfig();

        TranslationResultWindow.ApplyBehaviorFromConfig(saved);
        TranslationResultWindow.Instance.ApplyAppearance(saved.TranslationPanelUi);
        HistoryPanelWindow.Instance.ApplyAppearance(saved.HistoryPanelUi);

        if (HistoryPanelWindow.IsPanelVisible)
            HistoryPanelWindow.Instance.ReloadHistory(new HistoryStore());

        var modeHint = saved.TranslateOnSelection
            ? "选中即译已开启。"
            : saved.EnableHover
                ? "悬停翻译已开启。"
                : "仍可用热键/复制翻译。";
        _tray?.ShowBalloon(AppBranding.DisplayName, $"设置已保存。{modeHint}");
    }

    private void OnShowHistory() => Current.Dispatcher.Invoke(() =>
    {
        try { HistoryPanelWindow.EnsureVisible(); }
        catch (Exception ex) { ShowUiError("打开历史记录失败", ex); }
    });

    private void OnShowTranslation() => Current.Dispatcher.Invoke(() =>
    {
        try
        {
            TranslationResultWindow.OpenFromTray();
            TranslationResultWindow.Instance.ApplyAppearance(_configService!.Load().TranslationPanelUi);
        }
        catch (Exception ex) { ShowUiError("打开译文框失败", ex); }
    });

    private static void ShowUiError(string title, Exception ex)
    {
        StartupDiagnostics.LogException(title, ex);
        AppDialog.Warning(
            $"{title}：{ex.Message}\n\n详情已写入日志：\n{StartupDiagnostics.LogPath}",
            AppBranding.DisplayName);
    }

    private void OnExit()
    {
        FloatingPanelWindowActions.AllowPanelWindowsToClose = true;
        _clipboardTranslate?.Dispose();
        _hover?.Dispose();
        _hotkey?.Dispose();
        _tray?.Dispose();
        ReleaseSingleInstance();
        Shutdown();
    }

    private static bool TryAcquireSingleInstance()
    {
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            return createdNew;
        }
        catch
        {
            return true;
        }
    }

    private static void ReleaseSingleInstance()
    {
        try
        {
            if (_singleInstanceMutex is null) return;
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
        catch
        {
            // ignore
        }
    }
}
