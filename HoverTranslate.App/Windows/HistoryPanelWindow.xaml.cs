using System.Collections.ObjectModel;
using System.Text;
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
        FloatingPanelContextMenu.AttachHistoryListMenu(
            HistoryList,
            () => HistoryList.SelectedItem is HistoryItemVm item ? item.GetCopyText() : null,
            BuildAllHistoryCopyText);
        Hide();
    }

    private static string BuildAllHistoryCopyText()
    {
        var sb = new StringBuilder();
        foreach (HistoryItemVm item in Instance._items)
        {
            var block = item.GetCopyText();
            if (string.IsNullOrWhiteSpace(block)) continue;
            if (sb.Length > 0)
                sb.AppendLine().AppendLine("---").AppendLine();
            sb.Append(block);
        }

        return sb.ToString();
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
                    TargetPreview = "未开启历史保存，请在设置 → 历史窗 中勾选。",
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
        HistoryList.FontFamily = PanelAppearance.ResolveFontFamily(options.FontFamilyName);
        HistoryList.FontSize = Math.Round(12.0 * options.FontSizeScale, 1);
        HistoryCountText.Foreground = new System.Windows.Media.SolidColorBrush(colors.SubText);

        var showExtras = options.ShowExtras;
        HistoryCountText.Visibility = showExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        OpenInTranslationButton.Visibility = showExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        CopyHistoryButton.Visibility = System.Windows.Visibility.Visible;
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
        // 仅选中高亮，不自动打开译文窗或触发翻译（避免与悬停逻辑混淆）
    }

    private void OnCopySelected(object sender, System.Windows.RoutedEventArgs e)
    {
        string text;
        if (HistoryList.SelectedItem is HistoryItemVm item)
            text = item.GetCopyText();
        else
            text = BuildAllHistoryCopyText();

        if (string.IsNullOrWhiteSpace(text))
        {
            AppDialog.Info(
                HistoryList.SelectedItem is null
                    ? "暂无历史记录可复制。"
                    : "请先选中一条历史记录。",
                AppBranding.DisplayName,
                this);
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(text);
            CopyHistoryButton.Content = "已复制";
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"复制失败：{ex.Message}", AppBranding.DisplayName, this);
        }
    }

    private async void OnImportHistory(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "导入翻译历史",
            Filter = "JSON 文件|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var mode = await AppDialog.AskImportHistoryModeAsync(AppBranding.DisplayName, this).ConfigureAwait(true);
        if (mode is null)
            return;

        var merge = mode == AppDialog.ImportHistoryMode.Merge;

        IsEnabled = false;
        try
        {
            var path = dialog.FileName;
            var count = await System.Threading.Tasks.Task.Run(() =>
            {
                using var store = new HistoryStore();
                return store.ImportFromJsonFile(path, merge);
            }).ConfigureAwait(true);

            using var fresh = new HistoryStore();
            ReloadHistory(fresh);
            AppDialog.Info($"已导入 {count} 条记录。", owner: this);
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"导入失败：{ex.Message}", owner: this);
        }
        finally
        {
            IsEnabled = true;
        }
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

        public string GetCopyText() =>
            TranslationCopyText.Combine(FullSource, FullTarget);
    }
}
