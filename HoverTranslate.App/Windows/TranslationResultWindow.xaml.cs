using System.Windows;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

using HoverTranslate.App.Services;

namespace HoverTranslate.App.Windows;

public partial class TranslationResultWindow : System.Windows.Window
{
    private static TranslationResultWindow? _instance;

    public static bool IsPanelVisible => _instance is { Visibility: System.Windows.Visibility.Visible, IsVisible: true };

    /// <summary>用户手动关闭译文窗后为 true，直至再次通过托盘/设置打开。</summary>
    public static bool SuppressAutoShowUntilManualOpen { get; private set; }

    public static void ApplyBehaviorFromConfig(AppConfig config)
    {
        if (!config.SuppressPanelAfterUserClose)
            SuppressAutoShowUntilManualOpen = false;
    }

    public static TranslationResultWindow Instance
    {
        get
        {
            _instance ??= new TranslationResultWindow();
            return _instance;
        }
    }

    internal static void ResetInstanceIfClosed(Window window)
    {
        if (ReferenceEquals(_instance, window))
            _instance = null;
    }

    private readonly PanelBoundsState _bounds = new();

    private TranslationResultWindow()
    {
        InitializeComponent();
        FloatingPanelChrome.ApplyTitleBarButton(MinButton, ChromeIconKind.Minimize, "最小化到任务栏");
        FloatingPanelChrome.ApplyTitleBarButton(MaxButton, ChromeIconKind.Maximize, "最大化（铺满工作区）");
        FloatingPanelChrome.ApplyTitleBarButton(CloseButton, ChromeIconKind.Close, "关闭");
        FloatingPanelTitleBar.Wire(this, DragHandle, MinButton, MaxButton, CloseButton,
            () => FloatingPanelWindowActions.Minimize(this, RememberPlacement),
            () => PanelBoundsActions.ToggleMaximize(this, _bounds, MaxButton),
            OnClosePanel);
        FloatingPanelWindowActions.WireHideInsteadOfClose(this, OnClosePanel);
        FloatingPanelChrome.WireHiddenScroll(ContentScroll);
        WindowResizeHelper.Wire(ResizeRight, this, ResizeEdge.Right);
        WindowResizeHelper.Wire(ResizeBottom, this, ResizeEdge.Bottom);
        WindowResizeHelper.Wire(ResizeCorner, this, ResizeEdge.BottomRight);
        FloatingPanelContextMenu.AttachCopyMenu(TargetText, GetCombinedCopyText);
        FloatingPanelContextMenu.AttachCopyMenu(SourceText, GetCombinedCopyText);
        Hide();
    }

    private string GetCombinedCopyText() =>
        TranslationCopyText.Combine(
            SourceText.Visibility == System.Windows.Visibility.Visible ? SourceText.Text : null,
            TargetText.Text);

    private void RememberPlacement()
    {
        // 关闭时不写入配置：下次打开恢复默认大小与位置
    }

    private void OnClosePanel()
    {
        CancelAutoClose();
        var config = new ConfigService().Load();
        if (config.SuppressPanelAfterUserClose)
            SuppressAutoShowUntilManualOpen = true;

        OverlayWindow.HideActive();
        HoverGuard.PauseFor(1200);
        FloatingPanelWindowActions.HidePanel(this, RememberPlacement);
    }

    public static void EnsureVisible(bool clearSuppress = false)
    {
        if (clearSuppress)
            SuppressAutoShowUntilManualOpen = false;
        ShowPanelCore();
    }

    /// <summary>托盘/用户主动打开译文窗：解除「关闭后不再自动弹出大窗」。</summary>
    public static void OpenFromTray() => EnsureVisible(clearSuppress: true);

    private static void ShowPanelCore()
    {
        var w = Instance;
        var config = new ConfigService().Load();
        w.ResetLayout();
        w.ApplyAppearance(config.TranslationPanelUi);
        FloatingPanelWindowActions.RestoreFromTaskbar(w);
    }

    public static void HideIfVisible()
    {
        if (!IsPanelVisible) return;
        FloatingPanelWindowActions.HidePanel(Instance, () => { });
    }

    private void ResetLayout()
    {
        if (_bounds.IsMaximized)
            PanelBoundsActions.ToggleMaximize(this, _bounds, MaxButton);

        PanelLayout.ResetTranslationWindow(this);
    }

    public void ApplyAppearance(PanelChromeOptions options)
    {
        FloatingPanelChrome.ApplyAppearance(this, RootBorder, options, TargetText, SourceText);
        var colors = PanelAppearance.GetTheme(options.Theme);
        MachineTranslationHint.Foreground = new System.Windows.Media.SolidColorBrush(colors.SubText);
        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(colors.SubText);
        ExtrasPanel.Visibility = options.ShowExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public void SetLoading(string sourcePreview)
    {
        ApplyAppearance(new ConfigService().Load().TranslationPanelUi);
        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
        {
            StatusText.Text = "翻译中…";
            SetSourceVisible(sourcePreview);
        }
        else
            SetSourceVisible(null);

        TargetText.Text = "…";
        ContentScroll.ScrollToTop();
        CancelAutoClose();
    }

    public void ShowCurrent(TranslationResult result)
    {
        var config = new ConfigService().Load();
        CopyButton.Content = "复制译文";
        ApplyAppearance(config.TranslationPanelUi);

        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
        {
            var suffix = result.FormatStatusSuffix();
            StatusText.Text = string.IsNullOrEmpty(suffix) ? "翻译完成" : $"翻译完成 · {suffix}";
        }

        var source = result.SourceText;
        var target = result.TranslatedText;
        if (string.IsNullOrWhiteSpace(target))
            target = source;

        var showSource = !string.Equals(source.Trim(), target.Trim(), StringComparison.Ordinal);
        SetSourceVisible(showSource ? source : null);
        TargetText.Text = target;
        Visibility = System.Windows.Visibility.Visible;
        ContentScroll.ScrollToTop();
        if (config.OverlayTimeoutMs > 0)
            ScheduleAutoClose(config.OverlayTimeoutMs);
    }

    public void ShowCurrent(string source, string target, string provider)
    {
        ShowCurrent(TranslationResult.Ok(source, target, provider));
    }

    public void ShowError(string message)
    {
        var config = new ConfigService().Load();
        ApplyAppearance(config.TranslationPanelUi);
        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
            StatusText.Text = "提示";
        SetSourceVisible(null);
        TargetText.Text = message;
        Visibility = System.Windows.Visibility.Visible;
        ContentScroll.ScrollToTop();
        CancelAutoClose();
    }

    private System.Windows.Threading.DispatcherTimer? _autoCloseTimer;

    private void CancelAutoClose()
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
    }

    private void ScheduleAutoClose(int timeoutMs)
    {
        _autoCloseTimer?.Stop();
        if (timeoutMs <= 0) return;

        _autoCloseTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(timeoutMs)
        };
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer?.Stop();
            FloatingPanelWindowActions.HidePanel(this, RememberPlacement);
        };
        _autoCloseTimer.Start();
    }

    private void SetSourceVisible(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            SourceText.Visibility = System.Windows.Visibility.Collapsed;
            SourceText.Text = "";
            SourceText.Margin = new System.Windows.Thickness(0);
            return;
        }

        SourceText.Text = source;
        SourceText.Visibility = System.Windows.Visibility.Visible;
        SourceText.Margin = new System.Windows.Thickness(0, 0, 0, 6);
    }

    private void OnPlacementChanged(object sender, EventArgs e)
    {
        // 会话内可拖动缩放，但不持久化到 config
    }

    private void OnCopy(object sender, System.Windows.RoutedEventArgs e)
    {
        var text = TargetText.SelectedText;
        if (string.IsNullOrWhiteSpace(text))
            text = GetCombinedCopyText();
        if (string.IsNullOrWhiteSpace(text) || text == "…") return;

        try
        {
            ClipboardGuard.SetText(text);
            if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
                StatusText.Text = "已复制";
            else
                CopyButton.Content = "已复制";
        }
        catch
        {
            if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
                StatusText.Text = "复制失败";
        }
    }

}
