using System.Collections.ObjectModel;
using HoverTranslate.App;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Windows;

public partial class HistoryPanelWindow : System.Windows.Window
{
    private readonly ObservableCollection<HistoryItemVm> _items = new();
    private static HistoryPanelWindow? _instance;
    public static bool IsPanelVisible => _instance is { Visibility: System.Windows.Visibility.Visible, IsVisible: true };

    public static HistoryPanelWindow Instance
    {
        get
        {
            _instance ??= new HistoryPanelWindow();
            return _instance;
        }
    }

    private readonly PanelBoundsState _bounds = new();

    private HistoryPanelWindow()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _items;
        FloatingPanelChrome.ApplyTitleBarButton(MinButton, ChromeIconKind.Minimize, "最小化到任务栏");
        FloatingPanelChrome.ApplyTitleBarButton(MaxButton, ChromeIconKind.Maximize, "最大化（铺满工作区）");
        FloatingPanelChrome.ApplyTitleBarButton(CloseButton, ChromeIconKind.Close, "关闭");
        FloatingPanelTitleBar.Wire(this, DragHandle, MinButton, MaxButton, CloseButton,
            () => FloatingPanelWindowActions.Minimize(this, RememberPlacement),
            () => PanelBoundsActions.ToggleMaximize(this, _bounds, MaxButton),
            () => FloatingPanelWindowActions.HidePanel(this, RememberPlacement));
        FloatingPanelChrome.WireHiddenScroll(HistoryScroll);
        WindowResizeHelper.Wire(ResizeRight, this, ResizeEdge.Right);
        WindowResizeHelper.Wire(ResizeBottom, this, ResizeEdge.Bottom);
        WindowResizeHelper.Wire(ResizeCorner, this, ResizeEdge.BottomRight);
        Hide();
    }

    private void RememberPlacement()
    {
        // 关闭时不写入配置：下次打开恢复默认大小与位置
    }

    public static void EnsureVisible()
    {
        var w = Instance;
        var config = new ConfigService().Load();
        w.ResetLayout();
        w.ApplyAppearance(config.HistoryPanelUi);
        FloatingPanelWindowActions.RestoreFromTaskbar(w);

        try
        {
            if (!config.EnableHistory)
            {
                w._items.Clear();
                w._items.Add(new HistoryItemVm
                {
                    TargetPreview = "未开启历史保存，请在设置 → 通用 中勾选。",
                    SourcePreview = ""
                });
                w.UpdateFooterText(0);
                return;
            }

            w.ReloadHistory(new HistoryStore());
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"加载历史失败：{ex.Message}", AppBranding.DisplayName, Instance);
        }
    }

    private void ResetLayout()
    {
        if (_bounds.IsMaximized)
            PanelBoundsActions.ToggleMaximize(this, _bounds, MaxButton);

        PanelLayout.ResetHistoryWindow(this);
    }

    public void ApplyAppearance(PanelChromeOptions options)
    {
        FloatingPanelChrome.ApplyAppearance(this, RootBorder, options);
        var colors = PanelAppearance.GetTheme(options.Theme);
        HistoryList.Foreground = new System.Windows.Media.SolidColorBrush(colors.Text);
        HistoryCountText.Foreground = new System.Windows.Media.SolidColorBrush(colors.SubText);

        var showExtras = options.ShowExtras;
        HistoryCountText.Visibility = showExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        OpenInTranslationButton.Visibility = showExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public void ReloadHistory(HistoryStore store)
    {
        _items.Clear();
        var entries = store.GetRecent(80);
        foreach (var entry in entries)
        {
            var showSource = !string.Equals(entry.Source.Trim(), entry.Target.Trim(), StringComparison.Ordinal);
            _items.Add(new HistoryItemVm
            {
                SourcePreview = showSource ? entry.Source : "",
                TargetPreview = entry.Target,
                FullSource = entry.Source,
                FullTarget = entry.Target,
                Provider = entry.Provider
            });
        }

        UpdateFooterText(entries.Count);
    }

    private void UpdateFooterText(int count) =>
        HistoryCountText.Text = count == 0 ? "暂无记录" : $"共 {count} 条";

    private void OnHistorySelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryItemVm item) return;
        TranslationResultWindow.EnsureVisible();
        TranslationResultWindow.Instance.ShowCurrent(item.FullSource, item.FullTarget, item.Provider);
    }

    private void OnOpenInTranslation(object sender, System.Windows.RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryItemVm item) return;
        TranslationResultWindow.EnsureVisible();
        TranslationResultWindow.Instance.ShowCurrent(item.FullSource, item.FullTarget, item.Provider);
    }

    private void OnClearHistory(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!AppDialog.Confirm("确定清空全部历史记录？", AppBranding.DisplayName, this, dangerPrimary: true))
            return;

        var store = new HistoryStore();
        store.Clear();
        ReloadHistory(store);
    }

    private void OnPlacementChanged(object sender, EventArgs e)
    {
        // 会话内可拖动缩放，但不持久化到 config
    }

    private sealed class HistoryItemVm
    {
        public string SourcePreview { get; init; } = "";
        public string TargetPreview { get; init; } = "";
        public string FullSource { get; init; } = "";
        public string FullTarget { get; init; } = "";
        public string Provider { get; init; } = "";
    }
}
