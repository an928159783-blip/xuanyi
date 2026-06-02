using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HoverTranslate.App;
using HoverTranslate.App.Services;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;
using ListBox = System.Windows.Controls.ListBox;

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

    internal static void ResetInstanceIfClosed(Window window)
    {
        if (ReferenceEquals(_instance, window))
            _instance = null;
    }

    private readonly PanelBoundsState _bounds = new();

    private HistoryPanelWindow()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _items;
        HistoryList.SelectionMode = System.Windows.Controls.SelectionMode.Extended;
        HistoryList.PreviewKeyDown += OnHistoryListPreviewKeyDown;
        WireHistoryContextMenu();

        FloatingPanelChrome.ApplyTitleBarButton(MinButton, ChromeIconKind.Minimize, "最小化到任务栏");
        FloatingPanelChrome.ApplyTitleBarButton(MaxButton, ChromeIconKind.Maximize, "最大化（铺满工作区）");
        FloatingPanelChrome.ApplyTitleBarButton(CloseButton, ChromeIconKind.Close, "关闭");
        FloatingPanelTitleBar.Wire(this, DragHandle, MinButton, MaxButton, CloseButton,
            () => FloatingPanelWindowActions.Minimize(this, RememberPlacement),
            () => PanelBoundsActions.ToggleMaximize(this, _bounds, MaxButton),
            () => FloatingPanelWindowActions.HidePanel(this, RememberPlacement));
        FloatingPanelWindowActions.WireHideInsteadOfClose(this,
            () => FloatingPanelWindowActions.HidePanel(this, RememberPlacement));
        FloatingPanelChrome.WireHiddenScroll(HistoryScroll);
        WindowResizeHelper.Wire(ResizeRight, this, ResizeEdge.Right);
        WindowResizeHelper.Wire(ResizeBottom, this, ResizeEdge.Bottom);
        WindowResizeHelper.Wire(ResizeCorner, this, ResizeEdge.BottomRight);
        Hide();
    }

    private void WireHistoryContextMenu()
    {
        var menu = new ContextMenu();
        var copyOne = new MenuItem { Header = "复制本条" };
        copyOne.Click += (_, _) => CopySelectedEntries(selectAllFirst: false);
        var copyAll = new MenuItem { Header = "复制全部记录" };
        copyAll.Click += (_, _) => CopySelectedEntries(selectAllFirst: true);
        var selectAll = new MenuItem { Header = "全选" };
        selectAll.Click += (_, _) => HistoryList.SelectAll();
        menu.Items.Add(copyOne);
        menu.Items.Add(copyAll);
        menu.Items.Add(selectAll);
        menu.Opened += (_, _) =>
        {
            if (HistoryList.SelectedItems.Count == 0 && TryGetItemUnderMouse(out var item))
                HistoryList.SelectedItem = item;
        };
        HistoryList.ContextMenu = menu;
    }

    private void OnHistoryListPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            HistoryList.SelectAll();
            e.Handled = true;
        }
    }

    private bool TryGetItemUnderMouse(out HistoryItemVm? item)
    {
        item = null;
        var pos = HistoryList.MousePosition();
        var element = HistoryList.InputHitTest(pos) as DependencyObject;
        while (element is not null)
        {
            if (element is ListBoxItem { DataContext: HistoryItemVm vm })
            {
                item = vm;
                return true;
            }

            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    private static string BuildAllHistoryCopyText()
    {
        var sb = new StringBuilder();
        foreach (HistoryItemVm item in Instance._items)
        {
            var block = HistoryTextFormat.FormatExportBlock(item.FullSource, item.FullTarget);
            if (string.IsNullOrWhiteSpace(block)) continue;
            if (sb.Length > 0)
                sb.Append(HistoryTextFormat.EntryDelimiter);
            sb.Append(block);
        }

        return sb.ToString();
    }

    private void CopySelectedEntries(bool selectAllFirst)
    {
        if (_items.Count == 0 || _items[0].FullSource == "" && _items[0].PrimaryPreview.StartsWith("未开启"))
        {
            AppDialog.Info("暂无历史记录可复制。", AppBranding.DisplayName, this);
            return;
        }

        if (selectAllFirst)
        {
            HistoryList.SelectAll();
        }
        else
        {
            if (HistoryList.SelectedItems.Count == 0)
            {
                if (TryGetItemUnderMouse(out var under) && under is not null)
                    HistoryList.SelectedItem = under;
                else
                {
                    AppDialog.Info("请先选中一条历史记录。", AppBranding.DisplayName, this);
                    return;
                }
            }
            else if (HistoryList.SelectedItems.Count > 1)
            {
                // 复制本条：仅保留右键所在或最后选中的一条
                if (TryGetItemUnderMouse(out var under) && under is not null)
                {
                    HistoryList.SelectedItem = under;
                }
                else if (HistoryList.SelectedItem is HistoryItemVm one)
                {
                    HistoryList.SelectedItem = one;
                }
            }
        }

        string text;
        if (selectAllFirst)
            text = BuildAllHistoryCopyText();
        else if (HistoryList.SelectedItem is HistoryItemVm item)
            text = item.GetCopyText();
        else
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            ClipboardGuard.SetText(text);
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"复制失败：{ex.Message}", AppBranding.DisplayName, this);
        }
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
                    PrimaryPreview = "未开启历史保存，请在设置 → 历史窗 中勾选。",
                    SecondaryPreview = "",
                    FullSource = "",
                    FullTarget = "",
                    Provider = ""
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
        Background = System.Windows.Media.Brushes.Transparent;
        ResizeHost.Background = System.Windows.Media.Brushes.Transparent;
        HistoryScroll.Background = System.Windows.Media.Brushes.Transparent;
        HistoryList.Background = System.Windows.Media.Brushes.Transparent;

        var colors = PanelAppearance.GetTheme(options.Theme);
        HistoryList.Foreground = new System.Windows.Media.SolidColorBrush(colors.Text);
        HistoryList.FontFamily = PanelAppearance.ResolveFontFamily(options.FontFamilyName);
        HistoryList.FontSize = Math.Round(13.0 * options.FontSizeScale, 1);
        HistoryCountText.Foreground = new System.Windows.Media.SolidColorBrush(colors.SubText);

        HistoryCountText.Visibility = options.ShowExtras
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public void ReloadHistory(HistoryStore store)
    {
        _items.Clear();
        HistoryList.UnselectAll();
        var entries = store.GetRecent(80);
        foreach (var entry in entries)
        {
            var source = entry.Source?.Trim() ?? "";
            var target = entry.Target?.Trim() ?? "";
            var showBoth = !string.Equals(source, target, StringComparison.Ordinal);
            _items.Add(new HistoryItemVm
            {
                PrimaryPreview = source,
                SecondaryPreview = showBoth ? target : "",
                FullSource = entry.Source ?? "",
                FullTarget = entry.Target ?? "",
                Provider = entry.Provider
            });
        }

        UpdateFooterText(entries.Count);
    }

    private void UpdateFooterText(int count) =>
        HistoryCountText.Text = count == 0 ? "暂无记录" : $"共 {count} 条";

    private void OnHistorySelected(object sender, SelectionChangedEventArgs e)
    {
        // 选中高亮由 ListBox Extended 模式提供视觉反馈
    }

    private void OnExportHistory(object sender, System.Windows.RoutedEventArgs e)
    {
        var config = new ConfigService().Load();
        if (!config.EnableHistory)
        {
            AppDialog.Info("未开启历史保存，请在设置 → 历史窗 中勾选。", AppBranding.DisplayName, this);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出翻译历史",
            Filter = "文本文件（可读）|*.txt|JSON 备份（可导入）|*.json",
            FileName = $"xuanyi-history-{DateTime.Now:yyyyMMdd}.txt",
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var store = new HistoryStore();
            if (dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                store.ExportToJsonFile(dialog.FileName);
            else
                store.ExportToTextFile(dialog.FileName);
            AppDialog.Info($"已导出到：{dialog.FileName}", owner: this);
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"导出失败：{ex.Message}", owner: this);
        }
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
        public string PrimaryPreview { get; init; } = "";
        public string SecondaryPreview { get; init; } = "";
        public string FullSource { get; init; } = "";
        public string FullTarget { get; init; } = "";
        public string Provider { get; init; } = "";

        public string GetCopyText() =>
            TranslationCopyText.Combine(FullSource, FullTarget);
    }
}

internal static class ListBoxMouseExtensions
{
    public static System.Windows.Point MousePosition(this ListBox list) =>
        System.Windows.Input.Mouse.GetPosition(list);
}
