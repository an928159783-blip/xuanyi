using System.Windows;
using System.Windows.Threading;
using HoverTranslate.App.Services;
using HoverTranslate.App.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryAcquireSingleInstance())
        {
            System.Windows.MessageBox.Show(
                "炫译已在运行。\n\n请查看任务栏右下角托盘区（点击 ^ 展开隐藏图标），右键托盘图标可打开设置或退出。\n\n本程序没有传统主窗口，启动后只在托盘显示。",
                AppBranding.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.ToString(), $"{AppBranding.DisplayName} 错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            StartupDiagnostics.Log("OnStartup begin");
            _configService = new ConfigService();
            _configService.EnsureConfigExists();
            var config = _configService.Load();

            _coordinator = new TranslationCoordinator(_configService);
            _hover = new HoverTranslateService(_coordinator, _configService);
            _coordinator.AttachHover(_hover);

            _tray = new TrayService(
                _configService,
                config,
                () => OnTranslateRequested(),
                OnExit,
                OnShowHistory,
                OnShowTranslation,
                OnOpenSettings,
                ApplyHoverFromSettings);

            _hotkey = new HotkeyService();
            _hotkey.HotkeyPressed += (_, _) => OnTranslateRequested();
            try
            {
                _hotkey.Register(config.Hotkey);
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogException("Hotkey", ex);
                System.Windows.MessageBox.Show(
                    $"{ex.Message}\n将使用默认热键 {HotkeyParser.DefaultHotkey}。",
                    AppBranding.DisplayName, MessageBoxButton.OK, MessageBoxImage.Warning);
                _hotkey.Register(HotkeyParser.DefaultHotkey);
            }

            ApplyClipboardTranslate(config);
            ApplyHoverFromSettings(config.EnableHover || config.TranslateOnSelection);

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
                    System.Windows.MessageBox.Show(ex.ToString(), $"{AppBranding.DisplayName} 启动界面失败",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, DispatcherPriority.ApplicationIdle);

            _tray.ShowBalloon(AppBranding.DisplayName,
                config.TranslateOnCopy
                    ? $"已启动。{hk} 翻译；复制后自动翻译已开启。"
                    : $"已启动。{hk} 翻译选中内容。");
            StartupDiagnostics.Log("OnStartup ok");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("OnStartup", ex);
            System.Windows.MessageBox.Show(
                $"启动失败：{ex.Message}\n\n详情已写入：\n{StartupDiagnostics.LogPath}\n\n{ex}",
                AppBranding.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnTranslateRequested(string? knownText = null)
    {
        if (_coordinator is null) return;

        _clipboardTranslate?.PauseFor(4000);

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

            _ = _coordinator.TranslateFromSelectionOrClipboardAsync(captured);
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

        if (!config.TranslateOnCopy || _configService is null) return;

        _clipboardTranslate = new ClipboardTranslateService();
        _clipboardTranslate.Start(_configService, text => OnTranslateRequested(text));
    }

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

    private void OnSettingsSaved()
    {
        var config = _configService!.Load();
        ApplyHoverFromSettings(config.EnableHover || config.TranslateOnSelection);

        try
        {
            _hotkey?.ApplyHotkey(config.Hotkey);
        }
        catch (Exception ex)
        {
            ShowUiError("热键注册失败", ex);
        }

        ApplyClipboardTranslate(config);
        _tray?.RefreshConfig();

        if (TranslationResultWindow.IsPanelVisible)
            TranslationResultWindow.Instance.ApplyAppearance(config.TranslationPanelUi);
        if (HistoryPanelWindow.IsPanelVisible)
            HistoryPanelWindow.Instance.ApplyAppearance(config.HistoryPanelUi);

        var modeHint = config.TranslateOnSelection
            ? "选中即译已开启。"
            : config.EnableHover
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
            TranslationResultWindow.EnsureVisible();
            TranslationResultWindow.Instance.ApplyAppearance(_configService!.Load().TranslationPanelUi);
        }
        catch (Exception ex) { ShowUiError("打开译文框失败", ex); }
    });

    private static void ShowUiError(string title, Exception ex) =>
        System.Windows.MessageBox.Show($"{title}：{ex.Message}\n\n{ex}", AppBranding.DisplayName,
            MessageBoxButton.OK, MessageBoxImage.Warning);

    private void OnExit()
    {
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
