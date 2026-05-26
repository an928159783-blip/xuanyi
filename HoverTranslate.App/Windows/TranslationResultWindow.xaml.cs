using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Windows;

public partial class TranslationResultWindow : System.Windows.Window
{
    private static TranslationResultWindow? _instance;

    public static bool IsPanelVisible => _instance is { Visibility: System.Windows.Visibility.Visible, IsVisible: true };

    public static TranslationResultWindow Instance
    {
        get
        {
            _instance ??= new TranslationResultWindow();
            return _instance;
        }
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
        FloatingPanelChrome.WireHiddenScroll(ContentScroll);
        WindowResizeHelper.Wire(ResizeRight, this, ResizeEdge.Right);
        WindowResizeHelper.Wire(ResizeBottom, this, ResizeEdge.Bottom);
        WindowResizeHelper.Wire(ResizeCorner, this, ResizeEdge.BottomRight);
        Hide();
    }

    private void RememberPlacement()
    {
        // 关闭时不写入配置：下次打开恢复默认大小与位置
    }

    private void OnClosePanel()
    {
        CancelAutoClose();
        FloatingPanelWindowActions.HidePanel(this, RememberPlacement);
    }

    public static void EnsureVisible()
    {
        var w = Instance;
        var config = new ConfigService().Load();
        w.ResetLayout();
        w.ApplyAppearance(config.TranslationPanelUi);
        FloatingPanelWindowActions.RestoreFromTaskbar(w);
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
        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
        {
            var sub = PanelAppearance.GetTheme(options.Theme).SubText;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(sub);
        }
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

    public void ShowCurrent(string source, string target, string provider)
    {
        var config = new ConfigService().Load();
        ApplyAppearance(config.TranslationPanelUi);

        if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
            StatusText.Text = string.IsNullOrEmpty(provider) ? "翻译完成" : $"翻译完成 · {provider}";

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
        if (string.IsNullOrWhiteSpace(TargetText.Text) || TargetText.Text == "…") return;
        try
        {
            Clipboard.SetText(TargetText.Text);
            if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
                StatusText.Text = "已复制";
        }
        catch
        {
            if (ExtrasPanel.Visibility == System.Windows.Visibility.Visible)
                StatusText.Text = "复制失败";
        }
    }

}
