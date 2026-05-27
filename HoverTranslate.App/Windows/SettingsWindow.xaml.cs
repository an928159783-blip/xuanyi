using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HoverTranslate.App;
using HoverTranslate.App.Services;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly HistoryStore _historyStore = new();
    private AppConfig _working = new();
    private ApiProfile? _selectedProfile;
    private readonly Dictionary<string, System.Windows.Controls.CheckBox> _autoChecks = new();

    private static readonly ThemeOption[] ThemeItems =
    [
        new("dark", "深色（默认）"),
        new("light", "浅色明亮"),
        new("slate", "石板灰")
    ];

    private bool _uiReady;
    private bool _suppressTranslationOpacity;
    private bool _suppressHistoryOpacity;
    private bool _recordingHotkey;

    public event EventHandler? Saved;

    public SettingsWindow()
    {
        InitializeComponent();

        TranslationThemeCombo.ItemsSource = ThemeItems;
        HistoryThemeCombo.ItemsSource = ThemeItems;
        AddTemplateCombo.ItemsSource = ApiProfileKinds.Templates
            .Select(t => new TemplateItem(t.Kind, t.Label)).ToList();
        AddTemplateCombo.SelectedIndex = 0;

        ProfileKindCombo.ItemsSource = ApiProfileKinds.Templates
            .Select(t => new TemplateItem(t.Kind, t.Label)).ToList();
        ProfileKindCombo.DisplayMemberPath = "Label";
        ProfileKindCombo.SelectedValuePath = "Kind";

        TranslationOpacitySlider.ValueChanged += (_, _) =>
        {
            if (_suppressTranslationOpacity) return;
            SetTranslationOpacityPercent((int)TranslationOpacitySlider.Value, fromSlider: true);
        };
        HistoryOpacitySlider.ValueChanged += (_, _) =>
        {
            if (_suppressHistoryOpacity) return;
            SetHistoryOpacityPercent((int)HistoryOpacitySlider.Value, fromSlider: true);
        };
        TranslationThemeCombo.SelectionChanged += (_, _) => RefreshTranslationPreview();
        HistoryThemeCombo.SelectionChanged += (_, _) => RefreshHistoryPreview();

        Loaded += (_, _) =>
        {
            FloatingPanelChrome.WireAllScrollViewers(this);
            _uiReady = true;
            LoadFromDisk();
            ShowNavPanel(0);
        };
    }

    private void OnNavChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || NavList.SelectedIndex < 0) return;
        ShowNavPanel(NavList.SelectedIndex);
    }

    private void ShowNavPanel(int index)
    {
        PanelApi.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelTranslation.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelHistory.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        PanelHotkey.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        PanelPrivacy.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadFromDisk()
    {
        if (!_uiReady) return;
        _working = _configService.Load();
        ConfigService.MigratePanelUi(_working);
        ConfigMigration.MigrateBuiltInToProfiles(_working);

        RebuildProviderCombo();
        RebuildAutoProviderList();

        ProfileList.ItemsSource = _working.ApiProfiles.ToList();
        if (_working.ApiProfiles.Count > 0)
            ProfileList.SelectedIndex = 0;

        SanitizeCheck.IsChecked = _working.Sanitize;
        EnableHoverCheck.IsChecked = _working.EnableHover;
        HoverDelayBox.Text = _working.HoverDelayMs.ToString();
        EnableHistoryCheck.IsChecked = _working.EnableHistory;
        CloseAfterSaveCheck.IsChecked = _working.CloseSettingsAfterSave;
        HotkeyBox.Text = string.IsNullOrWhiteSpace(_working.Hotkey) ? HotkeyParser.DefaultHotkey : _working.Hotkey;
        TranslateOnCopyCheck.IsChecked = _working.TranslateOnCopy;
        OverlayTimeoutBox.Text = (_working.OverlayTimeoutMs / 1000).ToString();
        UpdateHistoryStatusUi();
        UpdateKindHint();

        SetTranslationOpacityPercent(PanelAppearance.ToPercent(_working.TranslationPanelUi.Opacity));
        SetHistoryOpacityPercent(PanelAppearance.ToPercent(_working.HistoryPanelUi.Opacity));
        TranslationExtrasCheck.IsChecked = _working.TranslationPanelUi.ShowExtras;
        HistoryExtrasCheck.IsChecked = _working.HistoryPanelUi.ShowExtras;

        SelectTheme(TranslationThemeCombo, _working.TranslationPanelUi.Theme);
        SelectTheme(HistoryThemeCombo, _working.HistoryPanelUi.Theme);

        RefreshTranslationPreview();
        RefreshHistoryPreview();
        OnProviderChanged(null!, null!);
    }

    private void SetTranslationOpacityPercent(int percent, bool fromSlider = false)
    {
        percent = Math.Clamp(percent, 0, 100);
        _suppressTranslationOpacity = true;
        TranslationOpacitySlider.Value = percent;
        if (!fromSlider || TranslationOpacityBox.Text != percent.ToString())
            TranslationOpacityBox.Text = percent.ToString();
        _suppressTranslationOpacity = false;
        RefreshTranslationPreview();
    }

    private void SetHistoryOpacityPercent(int percent, bool fromSlider = false)
    {
        percent = Math.Clamp(percent, 0, 100);
        _suppressHistoryOpacity = true;
        HistoryOpacitySlider.Value = percent;
        if (!fromSlider || HistoryOpacityBox.Text != percent.ToString())
            HistoryOpacityBox.Text = percent.ToString();
        _suppressHistoryOpacity = false;
        RefreshHistoryPreview();
    }

    private void OnTranslationOpacityBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TranslationOpacityBox.Text.Trim(), out var p))
            SetTranslationOpacityPercent(p);
        else
            SetTranslationOpacityPercent((int)TranslationOpacitySlider.Value);
    }

    private void OnHistoryOpacityBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(HistoryOpacityBox.Text.Trim(), out var p))
            SetHistoryOpacityPercent(p);
        else
            SetHistoryOpacityPercent((int)HistoryOpacitySlider.Value);
    }

    private static void SelectTheme(System.Windows.Controls.ComboBox combo, string? theme)
    {
        combo.SelectedValue = ThemeItems.Any(t => t.Id == theme) ? theme : "dark";
    }

    private void UpdateHistoryStatusUi()
    {
        var on = EnableHistoryCheck.IsChecked == true;
        HistoryStatusText.Text = on ? "当前状态：已开启 — 翻译会写入本机历史库" : "当前状态：已关闭 — 不会保存新记录";
        HistoryStatusDot.Fill = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(on ? "#22C55E" : "#94A3B8")!);
    }

    private void OnHistoryToggleChanged(object sender, RoutedEventArgs e) => UpdateHistoryStatusUi();

    private void RefreshTranslationPreview() =>
        ApplyPreview(TranslationPreview,
            TranslationThemeCombo.SelectedValue as string ?? "dark",
            PanelAppearance.FromPercent((int)TranslationOpacitySlider.Value));

    private void RefreshHistoryPreview() =>
        ApplyPreview(HistoryPreview,
            HistoryThemeCombo.SelectedValue as string ?? "dark",
            PanelAppearance.FromPercent((int)HistoryOpacitySlider.Value));

    private static void ApplyPreview(System.Windows.Controls.Border? preview, string theme, double opacity)
    {
        if (preview is null) return;
        var colors = PanelAppearance.GetTheme(theme);
        preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(opacity * 230), colors.Background.R, colors.Background.G, colors.Background.B));
        preview.BorderBrush = new SolidColorBrush(colors.Border);
        if (preview.Child is TextBlock tb)
            tb.Foreground = new SolidColorBrush(colors.Text);
    }

    private void OnTranslationOpacityPreset(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string s } && int.TryParse(s, out var p))
            SetTranslationOpacityPercent(p);
    }

    private void OnHistoryOpacityPreset(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string s } && int.TryParse(s, out var p))
            SetHistoryOpacityPercent(p);
    }

    private void RebuildProviderCombo()
    {
        var items = new List<ProviderItem> { new("auto", "Auto（多接口自动轮询）") };
        items.AddRange(_working.ApiProfiles.Select(p =>
            new ProviderItem(p.Id, $"{p.Name} ({ProviderResolver.KindLabel(p.Kind)})")));
        ProviderCombo.ItemsSource = items;
        ProviderCombo.DisplayMemberPath = "Label";
        ProviderCombo.SelectedValuePath = "Id";
        var selected = items.FirstOrDefault(i => i.Id == _working.Provider) ?? items[0];
        ProviderCombo.SelectedItem = selected;
    }

    private void RebuildAutoProviderList()
    {
        _autoChecks.Clear();
        AutoProviderPanel.Children.Clear();
        foreach (var entry in ProviderResolver.ListAllProviders(_working))
        {
            var cb = new System.Windows.Controls.CheckBox
            {
                Content = entry.HasKey ? entry.Label : $"{entry.Label}（未填 Key）",
                IsEnabled = entry.HasKey,
                IsChecked = _working.AutoProviderIds.Count == 0 || _working.AutoProviderIds.Contains(entry.Id)
            };
            _autoChecks[entry.Id] = cb;
            AutoProviderPanel.Children.Add(cb);
        }
    }

    private void OnProviderChanged(object sender, SelectionChangedEventArgs e)
    {
        var isAuto = ProviderCombo.SelectedItem is ProviderItem { Id: "auto" };
        AutoPanel.Visibility = isAuto ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
    {
        FlushSelectedProfile();
        if (ProfileList.SelectedItem is not ApiProfile profile)
        {
            _selectedProfile = null;
            return;
        }

        _selectedProfile = profile;
        ProfileNameBox.Text = profile.Name;
        ProfileKeyBox.Password = profile.ApiKey;
        ProfileUrlBox.Text = profile.BaseUrl;
        ProfileModelBox.Text = profile.Model;
        ProfileRegionBox.Text = profile.Region;
        ProfileKindCombo.SelectedValue = profile.Kind;
        OnProfileKindChanged(null!, null!);
    }

    private void FlushSelectedProfile()
    {
        if (_selectedProfile is null) return;
        _selectedProfile.Name = ProfileNameBox.Text.Trim();
        _selectedProfile.ApiKey = ProfileKeyBox.Password;
        _selectedProfile.BaseUrl = ProfileUrlBox.Text.Trim();
        _selectedProfile.Model = ProfileModelBox.Text.Trim();
        _selectedProfile.Region = ProfileRegionBox.Text.Trim();
        if (ProfileKindCombo.SelectedValue is string kind)
            _selectedProfile.Kind = kind;
    }

    private void OnProfileKindChanged(object sender, SelectionChangedEventArgs e)
    {
        var kind = ProfileKindCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible;
        ProfileOpenAiPanel.Visibility = kind == ApiProfileKinds.OpenAiCompatible
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProfileRegionPanel.Visibility = kind == ApiProfileKinds.Microsoft
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateKindHintForKind(kind);
    }

    private void OnAddTemplateChanged(object sender, SelectionChangedEventArgs e) => UpdateKindHint();

    private void UpdateKindHint() =>
        UpdateKindHintForKind(AddTemplateCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible);

    private void UpdateKindHintForKind(string kind)
    {
        var hint = ApiProfileKinds.Templates.FirstOrDefault(t => t.Kind == kind).Hint
                   ?? "请填写 API Key。";
        KindHintText.Text = hint;
    }

    private void OnAddProfile(object sender, RoutedEventArgs e)
    {
        var kind = AddTemplateCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible;
        var profile = CreateProfileFromTemplate(kind);
        _working.ApiProfiles.Add(profile);
        RefreshProfileList(profile);
    }

    private static ApiProfile CreateProfileFromTemplate(string kind) => kind switch
    {
        ApiProfileKinds.Microsoft => new ApiProfile
        {
            Name = "微软翻译",
            Kind = kind,
            Region = "global"
        },
        ApiProfileKinds.Google => new ApiProfile
        {
            Name = "谷歌翻译",
            Kind = kind
        },
        _ => new ApiProfile
        {
            Name = "OpenAI 兼容",
            Kind = ApiProfileKinds.OpenAiCompatible,
            BaseUrl = "https://api.deepseek.com",
            Model = "deepseek-chat"
        }
    };

    private void RefreshProfileList(ApiProfile? select = null)
    {
        FlushSelectedProfile();
        ProfileList.ItemsSource = null;
        ProfileList.ItemsSource = _working.ApiProfiles.ToList();
        if (select is not null)
            ProfileList.SelectedItem = select;
        RebuildProviderCombo();
        RebuildAutoProviderList();
    }

    private void OnDeleteProfile(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is not ApiProfile profile) return;
        if (System.Windows.MessageBox.Show($"删除接口「{profile.Name}」？", AppBranding.SettingsTitle,
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _working.ApiProfiles.Remove(profile);
        _working.AutoProviderIds.Remove(profile.Id);
        if (_working.Provider == profile.Id)
            _working.Provider = "auto";
        _selectedProfile = null;
        RefreshProfileList();
    }

    private void OnOpenProviderHelp(object sender, RoutedEventArgs e)
    {
        var kind = ProfileKindCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible;
        var url = kind switch
        {
            ApiProfileKinds.Microsoft => "https://portal.azure.com/#create/Microsoft.CognitiveServicesTextTranslation",
            ApiProfileKinds.Google => "https://console.cloud.google.com/apis/library/translate.googleapis.com",
            _ => "https://dashscope.console.aliyun.com/apiKey"
        };
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法打开浏览器：{ex.Message}\n{url}", AppBranding.SettingsTitle);
        }
    }

    private void OnOpenConfigDir(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(ConfigService.ConfigDirectory);
        Process.Start(new ProcessStartInfo(ConfigService.ConfigDirectory) { UseShellExecute = true });
    }

    private void OnExportHistory(object sender, RoutedEventArgs e)
    {
        if (!_working.EnableHistory)
        {
            System.Windows.MessageBox.Show("请先在上方勾选「启用本地翻译历史」。", AppBranding.SettingsTitle,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出翻译历史",
            Filter = "JSON 文件|*.json",
            FileName = $"xuanyi-history-{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _historyStore.ExportToJsonFile(dialog.FileName);
            System.Windows.MessageBox.Show($"已导出到：{dialog.FileName}", AppBranding.SettingsTitle);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"导出失败：{ex.Message}", AppBranding.SettingsTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnExportDiagnostics(object sender, RoutedEventArgs e) =>
        Services.DiagnosticExportService.ExportWithPrompt();

    private void OnClearHistory(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("确定清空本机全部翻译历史？", AppBranding.SettingsTitle,
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        _historyStore.Clear();
        System.Windows.MessageBox.Show("历史记录已清空。", AppBranding.SettingsTitle);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        OnTranslationOpacityBoxLostFocus(this, e);
        OnHistoryOpacityBoxLostFocus(this, e);
        FlushSelectedProfile();

        if (ProviderCombo.SelectedItem is ProviderItem item)
            _working.Provider = item.Id;

        if (_working.Provider.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            _working.AutoProviderIds = _autoChecks
                .Where(kv => kv.Value.IsChecked == true)
                .Select(kv => kv.Key)
                .ToList();
        }

        _working.Sanitize = SanitizeCheck.IsChecked == true;
        _working.EnableHover = EnableHoverCheck.IsChecked == true;
        if (!int.TryParse(HoverDelayBox.Text.Trim(), out var delay) || delay < 200)
            delay = 800;
        _working.HoverDelayMs = Math.Min(5000, delay);
        _working.EnableHistory = EnableHistoryCheck.IsChecked == true;
        _working.CloseSettingsAfterSave = CloseAfterSaveCheck.IsChecked == true;
        _working.TranslateOnCopy = TranslateOnCopyCheck.IsChecked == true;

        if (int.TryParse(OverlayTimeoutBox.Text.Trim(), out var sec) && sec >= 0)
            _working.OverlayTimeoutMs = sec * 1000;
        else
            _working.OverlayTimeoutMs = 0;

        var hotkey = HotkeyBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(hotkey))
            hotkey = HotkeyParser.DefaultHotkey;
        try
        {
            HotkeyParser.Parse(hotkey);
            _working.Hotkey = hotkey;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"热键无效：{ex.Message}", AppBranding.SettingsTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _working.TranslationPanelUi.Opacity = PanelAppearance.FromPercent((int)TranslationOpacitySlider.Value);
        _working.TranslationPanelUi.Theme = TranslationThemeCombo.SelectedValue as string ?? "dark";
        _working.TranslationPanelUi.ShowExtras = TranslationExtrasCheck.IsChecked == true;
        _working.HistoryPanelUi.Opacity = PanelAppearance.FromPercent((int)HistoryOpacitySlider.Value);
        _working.HistoryPanelUi.Theme = HistoryThemeCombo.SelectedValue as string ?? "dark";
        _working.HistoryPanelUi.ShowExtras = HistoryExtrasCheck.IsChecked == true;

        _configService.Save(_working);
        Saved?.Invoke(this, EventArgs.Empty);

        SaveHintText.Text = "已保存";
        SaveHintText.Visibility = Visibility.Visible;

        if (CloseAfterSaveCheck.IsChecked == true)
            Close();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnRecordHotkey(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = true;
        HotkeyHintText.Text = "正在录制：请按下组合键（需含 Ctrl / Shift / Alt 之一）…";
        PreviewKeyDown += OnHotkeyPreviewKeyDown;
        Focus();
    }

    private void OnHotkeyPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_recordingHotkey) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (!HotkeyCapture.TryFormat(Keyboard.Modifiers, key, out var hotkey))
            return;

        HotkeyBox.Text = hotkey;
        _recordingHotkey = false;
        HotkeyHintText.Text = "录制：点击「按下录制」后，在键盘上按下组合键（需含 Ctrl/Shift/Alt 之一）";
        PreviewKeyDown -= OnHotkeyPreviewKeyDown;
    }

    private void OnResetHotkey(object sender, RoutedEventArgs e) =>
        HotkeyBox.Text = HotkeyParser.DefaultHotkey;

    private sealed record ThemeOption(string Id, string Label);
    private sealed record ProviderItem(string Id, string Label);
    private sealed record TemplateItem(string Kind, string Label);
}
