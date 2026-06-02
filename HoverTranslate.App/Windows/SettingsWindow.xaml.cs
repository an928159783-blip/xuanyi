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
        new("system", "跟随系统"),
        new("dark", "深色"),
        new("light", "浅色"),
        new("slate", "石板灰")
    ];

    private static readonly FontFamilyOption[] FontFamilyItems =
    [
        new("", "默认（微软雅黑 / Segoe UI）"),
        new("Microsoft YaHei UI", "微软雅黑 UI"),
        new("Microsoft YaHei", "微软雅黑"),
        new("Segoe UI", "Segoe UI"),
        new("SimSun", "宋体"),
        new("KaiTi", "楷体")
    ];

    private static readonly DirectionOption[] TranslationDirectionItems =
    [
        new(TranslationDirectionResolver.ModeAuto, "自动（英↔中）"),
        new(TranslationDirectionResolver.ModeEnToZh, "英文 → 中文"),
        new(TranslationDirectionResolver.ModeZhToEn, "中文 → 英文")
    ];

    private bool _uiReady;
    private bool _suppressTranslationOpacity;
    private bool _suppressHistoryOpacity;
    private bool _suppressGeneralFontScale;
    private bool _recordingHotkey;
    private bool _recordingScreenshotHotkey;

    public event EventHandler<AppConfig>? Saved;

    public SettingsWindow()
    {
        InitializeComponent();

        ProfileListHost.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnProfileListPreviewMouseWheel), true);
        ProfileList.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnProfileListPreviewMouseWheel), true);

        TranslationThemeCombo.ItemsSource = ThemeItems;
        HistoryThemeCombo.ItemsSource = ThemeItems;
        GeneralThemeCombo.ItemsSource = ThemeItems;
        GeneralFontFamilyCombo.ItemsSource = FontFamilyItems;
        TranslationDirectionCombo.ItemsSource = TranslationDirectionItems;
        AddTemplateCombo.ItemsSource = ApiProfileAddPresets.All;
        AddTemplateCombo.DisplayMemberPath = "Label";
        AddTemplateCombo.SelectedValuePath = "Id";
        AddTemplateCombo.SelectedIndex = 0;

        ProfileKindCombo.ItemsSource = ApiProfileKinds.Templates
            .Select(t => new TemplateItem(t.Kind, t.Label)).ToList();
        ProfileKindCombo.DisplayMemberPath = "Label";
        ProfileKindCombo.SelectedValuePath = "Kind";

        TranslationOpacitySlider.ValueChanged += (_, _) =>
        {
            if (_suppressTranslationOpacity) return;
            SetTranslationOpacityPercent((int)TranslationOpacitySlider.Value);
        };
        HistoryOpacitySlider.ValueChanged += (_, _) =>
        {
            if (_suppressHistoryOpacity) return;
            SetHistoryOpacityPercent((int)HistoryOpacitySlider.Value);
        };
        GeneralFontScaleSlider.ValueChanged += (_, _) =>
        {
            if (_suppressGeneralFontScale) return;
            SetGeneralFontScalePercent((int)GeneralFontScaleSlider.Value, fromSlider: true);
        };
        HistoryThemeCombo.SelectionChanged += (_, _) =>
        {
            if (!_uiReady) return;
            RefreshHistoryPreview();
            PreviewHistoryPanelOpacity((int)HistoryOpacitySlider.Value);
        };
        TranslationThemeCombo.SelectionChanged += (_, _) =>
        {
            if (!_uiReady) return;
            RefreshTranslationPreview();
            PreviewTranslationPanelOpacity((int)TranslationOpacitySlider.Value);
        };

        Loaded += (_, _) =>
        {
            FloatingPanelChrome.WireAllScrollViewers(this);
            AboutVersionText.Text = $"版本 {UpdateCheckService.CurrentVersion} · {AppBuildInfo.FormatForDisplay()}";
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

    public void SelectNav(int index)
    {
        if (index < 0 || index >= NavList.Items.Count) return;
        NavList.SelectedIndex = index;
        ShowNavPanel(index);
    }

    private void ShowNavPanel(int index)
    {
        PanelApi.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelFloatWindows.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelHotkey.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        PanelGeneral.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        PanelPrivacy.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
        PanelAbout.Visibility = index == 5 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnOpenFeatureGuide(object sender, RoutedEventArgs e) =>
        SettingsWindowHost.ShowFeatureGuide(this);

    private void LoadFromDisk()
    {
        if (!_uiReady) return;
        _working = _configService.Load();
        ConfigService.MigratePanelUi(_working);
        ConfigMigration.MigrateBuiltInToProfiles(_working);

        RebuildProviderCombo();
        RebuildAutoProviderList();

        ProfileList.ItemsSource = BuildProfileListItems();
        if (_working.ApiProfiles.Count > 0)
            ProfileList.SelectedIndex = 0;

        SanitizeCheck.IsChecked = _working.Sanitize;
        EnableHoverCheck.IsChecked = _working.EnableHover;
        HoverDelayBox.Text = _working.HoverDelayMs.ToString();
        EnableHistoryCheck.IsChecked = _working.EnableHistory;
        ShowPanelOnTranslateCheck.IsChecked = _working.ShowPanelOnTranslate;
        ShowPanelOnHoverCheck.IsChecked = _working.ShowPanelOnHover;
        SuppressPanelAfterCloseCheck.IsChecked = _working.SuppressPanelAfterUserClose;
        HideSettingsForScreenshotPickCheck.IsChecked = _working.HideSettingsForScreenshotPick;
        CloseAfterSaveCheck.IsChecked = _working.CloseSettingsAfterSave;
        ShowStartupNoticeCheck.IsChecked = _working.ShowStartupNotice;
        HotkeyBox.Text = string.IsNullOrWhiteSpace(_working.Hotkey) ? HotkeyParser.DefaultHotkey : _working.Hotkey;
        TranslateOnSelectionCheck.IsChecked = _working.TranslateOnSelection;
        TranslateOnCopyCheck.IsChecked = _working.TranslateOnCopy;
        EnableScreenshotHotkeyCheck.IsChecked = _working.EnableScreenshotRegionHotkey;
        ScreenshotHotkeyBox.Text = string.IsNullOrWhiteSpace(_working.ScreenshotRegionHotkey)
            ? "Ctrl+Shift+S"
            : _working.ScreenshotRegionHotkey;
        SelectTranslationDirection(_working.TranslationDirection);
        OverlayTimeoutBox.Text = (_working.OverlayTimeoutMs / 1000).ToString();
        UpdateHistoryStatusUi();
        UpdateKindHint();

        SetTranslationOpacityPercent(PanelAppearance.ToPercent(_working.TranslationPanelUi.Opacity));
        SetHistoryOpacityPercent(PanelAppearance.ToPercent(_working.HistoryPanelUi.Opacity));
        TranslationExtrasCheck.IsChecked = _working.TranslationPanelUi.ShowExtras;
        HistoryExtrasCheck.IsChecked = _working.HistoryPanelUi.ShowExtras;

        SelectTheme(TranslationThemeCombo, _working.TranslationPanelUi.Theme);
        SelectTheme(HistoryThemeCombo, _working.HistoryPanelUi.Theme);
        SelectTheme(GeneralThemeCombo, _working.TranslationPanelUi.Theme);
        SelectFontFamily(GeneralFontFamilyCombo, _working.TranslationPanelUi.FontFamilyName);
        SetGeneralFontScalePercent((int)Math.Round(_working.TranslationPanelUi.FontSizeScale * 100));

        RefreshTranslationPreview();
        RefreshHistoryPreview();
        OnProviderChanged(null!, null!);
        UpdateKeyHelpUi();
    }

    private void OnProfileEditorChanged(object sender, TextChangedEventArgs e)
    {
        UpdateKeyHelpUi();
        if (_selectedProfile is not null && sender == ProfileModelBox && !string.IsNullOrWhiteSpace(ProfileModelBox.Text))
            ProfileNameBox.Text = ProfileModelBox.Text.Trim();
    }

    private void OnProfileListPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scroll = FindVisualChild<ScrollViewer>(ProfileList);
        if (scroll is null || scroll.ScrollableHeight <= 0)
            return;

        var atTop = scroll.VerticalOffset <= 0.5;
        var atBottom = scroll.VerticalOffset >= scroll.ScrollableHeight - 0.5;
        var scrollingUp = e.Delta > 0;
        var scrollingDown = e.Delta < 0;

        if ((scrollingUp && atTop) || (scrollingDown && atBottom))
            return;

        var next = scroll.VerticalOffset - e.Delta;
        next = Math.Max(0, Math.Min(scroll.ScrollableHeight, next));
        scroll.ScrollToVerticalOffset(next);
        e.Handled = true;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private void SetTranslationOpacityPercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        _suppressTranslationOpacity = true;
        TranslationOpacitySlider.Value = percent;
        TranslationOpacityBox.Text = percent.ToString();
        _suppressTranslationOpacity = false;
        RefreshTranslationPreview();
        PreviewTranslationPanelOpacity(percent);
    }

    private void SetHistoryOpacityPercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        _suppressHistoryOpacity = true;
        HistoryOpacitySlider.Value = percent;
        HistoryOpacityBox.Text = percent.ToString();
        _suppressHistoryOpacity = false;
        RefreshHistoryPreview();
        PreviewHistoryPanelOpacity(percent);
    }

    private void PreviewTranslationPanelOpacity(int percent)
    {
        if (!_uiReady) return;
        var options = BuildTranslationPanelPreviewOptions(percent);
        TranslationResultWindow.Instance.ApplyAppearance(options);
    }

    private void PreviewHistoryPanelOpacity(int percent)
    {
        if (!_uiReady) return;
        var options = BuildHistoryPanelPreviewOptions(percent);
        HistoryPanelWindow.Instance.ApplyAppearance(options);
    }

    private PanelChromeOptions BuildTranslationPanelPreviewOptions(int percent) => new()
    {
        Opacity = PanelAppearance.FromPercent(percent),
        Theme = TranslationThemeCombo.SelectedValue as string ?? "dark",
        FontSizeScale = ConfigService.ClampFontSizeScale(GeneralFontScaleSlider.Value / 100.0),
        FontFamilyName = GeneralFontFamilyCombo.SelectedValue as string ?? "",
        ShowExtras = TranslationExtrasCheck.IsChecked == true
    };

    private PanelChromeOptions BuildHistoryPanelPreviewOptions(int percent) => new()
    {
        Opacity = PanelAppearance.FromPercent(percent),
        Theme = HistoryThemeCombo.SelectedValue as string ?? "dark",
        FontSizeScale = ConfigService.ClampFontSizeScale(GeneralFontScaleSlider.Value / 100.0),
        FontFamilyName = GeneralFontFamilyCombo.SelectedValue as string ?? "",
        ShowExtras = HistoryExtrasCheck.IsChecked == true
    };

    private void OnOpacityBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;

        if (ReferenceEquals(sender, TranslationOpacityBox))
            OnTranslationOpacityBoxLostFocus(TranslationOpacityBox, e);
        else if (ReferenceEquals(sender, HistoryOpacityBox))
            OnHistoryOpacityBoxLostFocus(HistoryOpacityBox, e);
        else if (ReferenceEquals(sender, GeneralFontScaleBox))
            OnGeneralFontScaleBoxLostFocus(GeneralFontScaleBox, e);

        e.Handled = true;
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

    private void SetGeneralFontScalePercent(int percent, bool fromSlider = false)
    {
        percent = Math.Clamp(percent, 90, 200);
        _suppressGeneralFontScale = true;
        GeneralFontScaleSlider.Value = percent;
        if (!fromSlider || GeneralFontScaleBox.Text != percent.ToString())
            GeneralFontScaleBox.Text = percent.ToString();
        _suppressGeneralFontScale = false;
        PreviewFloatingFontScale();
    }

    private void PreviewFloatingFontScale()
    {
        if (!_uiReady) return;

        var globalTheme = GeneralThemeCombo.SelectedValue as string ?? "dark";
        var globalScale = ConfigService.ClampFontSizeScale(GeneralFontScaleSlider.Value / 100.0);
        var globalFont = GeneralFontFamilyCombo.SelectedValue as string ?? "";

        TranslationResultWindow.Instance.ApplyAppearance(new PanelChromeOptions
        {
            Opacity = PanelAppearance.FromPercent((int)TranslationOpacitySlider.Value),
            Theme = TranslationThemeCombo.SelectedValue as string ?? globalTheme,
            FontSizeScale = globalScale,
            FontFamilyName = globalFont,
            ShowExtras = TranslationExtrasCheck.IsChecked == true
        });
        HistoryPanelWindow.Instance.ApplyAppearance(new PanelChromeOptions
        {
            Opacity = PanelAppearance.FromPercent((int)HistoryOpacitySlider.Value),
            Theme = HistoryThemeCombo.SelectedValue as string ?? globalTheme,
            FontSizeScale = globalScale,
            FontFamilyName = globalFont,
            ShowExtras = HistoryExtrasCheck.IsChecked == true
        });
    }

    private void OnGeneralFontScaleBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(GeneralFontScaleBox.Text.Trim(), out var p))
            SetGeneralFontScalePercent(p);
        else
            SetGeneralFontScalePercent((int)GeneralFontScaleSlider.Value);
    }

    private void OnGeneralFontScalePreset(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string tag } && int.TryParse(tag, out var p))
            SetGeneralFontScalePercent(p);
    }

    private void OnGeneralThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || GeneralThemeCombo.SelectedValue is not string theme) return;
        SelectTheme(TranslationThemeCombo, theme);
        SelectTheme(HistoryThemeCombo, theme);
        RefreshTranslationPreview();
        RefreshHistoryPreview();
    }

    private static void SelectTheme(System.Windows.Controls.ComboBox combo, string? theme)
    {
        var id = ThemeItems.Any(t => t.Id == theme) ? theme! : "dark";
        combo.SelectedValue = id;
        if (combo.SelectedValue is null)
            combo.SelectedIndex = Math.Max(0, Array.FindIndex(ThemeItems, t => t.Id == id));
    }

    private static void SelectFontFamily(System.Windows.Controls.ComboBox combo, string? name)
    {
        var id = name ?? "";
        if (!FontFamilyItems.Any(t => t.Id == id))
            id = "";
        combo.SelectedValue = id;
        if (combo.SelectedValue is null)
            combo.SelectedIndex = 0;
    }

    private void OnGeneralFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady) return;
        PreviewFloatingFontScale();
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
        var bgAlpha = PanelAppearance.ToBackgroundAlpha(opacity);
        preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            bgAlpha, colors.Background.R, colors.Background.G, colors.Background.B));
        var borderAlpha = (byte)Math.Clamp((int)Math.Round(bgAlpha * 0.72), 0, 255);
        preview.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            borderAlpha, colors.Border.R, colors.Border.G, colors.Border.B));
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
            new ProviderItem(p.Id, ApiProfileDisplay.GetChannelLabel(p))));
        ProviderCombo.ItemsSource = items;
        ProviderCombo.DisplayMemberPath = "Label";
        ProviderCombo.SelectedValuePath = "Id";
        var selected = items.FirstOrDefault(i => i.Id == _working.Provider) ?? items[0];
        ProviderCombo.SelectedItem = selected;
        UpdateActiveProviderStatus();
    }

    private void UpdateActiveProviderStatus()
    {
        if (!_uiReady || ActiveProviderStatusText is null) return;
        ActiveProviderStatusText.Text = TranslationUsageTracker.FormatForSettings(_working);
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
        if (ProviderCombo.SelectedItem is ProviderItem item)
            _working.Provider = item.Id;
        UpdateActiveProviderStatus();
    }

    private static List<ProfileListItemVm> BuildProfileListItems(AppConfig config) =>
        config.ApiProfiles.Select(p => new ProfileListItemVm
        {
            Profile = p,
            PrimaryLabel = ApiProfileDisplay.GetPrimaryLabel(p),
            SecondaryLabel = ApiProfileDisplay.GetSecondaryLabel(p)
        }).ToList();

    private List<ProfileListItemVm> BuildProfileListItems() => BuildProfileListItems(_working);

    private ApiProfile? GetSelectedProfile() =>
        ProfileList.SelectedItem is ProfileListItemVm item ? item.Profile : null;

    private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
    {
        FlushSelectedProfile();
        if (GetSelectedProfile() is not ApiProfile profile)
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
        ProfileTypeText.Text = ApiProfileDisplay.GetProviderBrandLabel(profile);
        OnProfileKindChanged(null!, null!);
        UpdateKeyHelpUi();
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

        if (!string.IsNullOrWhiteSpace(_selectedProfile.Model)
            && (string.IsNullOrWhiteSpace(_selectedProfile.Name)
                || _selectedProfile.Name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
                || _selectedProfile.Name.Contains("兼容", StringComparison.OrdinalIgnoreCase)))
        {
            _selectedProfile.Name = _selectedProfile.Model.Trim();
        }
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
        UpdateKeyHelpUi();
    }

    private void OnAddTemplateChanged(object sender, SelectionChangedEventArgs e) => UpdateKindHint();

    private void UpdateKindHint()
    {
        if (AddTemplateCombo.SelectedItem is ApiProfileAddPresets.Preset preset)
            KindHintText.Text = preset.Hint;
        else
            UpdateKindHintForKind(ApiProfileKinds.OpenAiCompatible);
    }

    private void UpdateKindHintForKind(string kind)
    {
        if (AddTemplateCombo.SelectedItem is ApiProfileAddPresets.Preset preset
            && preset.Kind == kind
            && ProfileKindCombo.SelectedValue as string == kind)
        {
            KindHintText.Text = preset.Hint;
            return;
        }

        var hint = ApiProfileKinds.Templates.FirstOrDefault(t => t.Kind == kind).Hint
                   ?? "请填写 API Key。";
        KindHintText.Text = hint;
    }

    private void OnAddProfile(object sender, RoutedEventArgs e)
    {
        var presetId = AddTemplateCombo.SelectedValue as string ?? ApiProfileAddPresets.All[0].Id;
        var profile = CreateProfileFromPreset(presetId);
        _working.ApiProfiles.Add(profile);
        RefreshProfileList(profile);
    }

    private static ApiProfile CreateProfileFromPreset(string presetId)
    {
        var preset = ApiProfileAddPresets.Get(presetId);
        return preset.Kind switch
        {
            ApiProfileKinds.Microsoft => new ApiProfile
            {
                Name = preset.DefaultName,
                Kind = preset.Kind,
                Region = "global"
            },
            ApiProfileKinds.Google => new ApiProfile
            {
                Name = preset.DefaultName,
                Kind = preset.Kind
            },
            _ => new ApiProfile
            {
                Name = preset.DefaultName,
                Kind = ApiProfileKinds.OpenAiCompatible,
                BaseUrl = preset.DefaultBaseUrl,
                Model = preset.DefaultModel
            }
        };
    }

    private void RefreshProfileList(ApiProfile? select = null)
    {
        FlushSelectedProfile();
        var items = BuildProfileListItems();
        ProfileList.ItemsSource = null;
        ProfileList.ItemsSource = items;
        if (select is not null)
            ProfileList.SelectedItem = items.FirstOrDefault(i => i.Profile.Id == select.Id);
        RebuildProviderCombo();
        RebuildAutoProviderList();
    }

    private void OnDeleteProfileFromList(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is System.Windows.Controls.Button { Tag: ApiProfile profile })
            DeleteProfile(profile);
    }

    private void OnProfileContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (GetSelectedProfile() is null)
            e.Handled = true;
    }

    private void OnDeleteProfileFromContext(object sender, RoutedEventArgs e)
    {
        if (GetSelectedProfile() is ApiProfile profile)
            DeleteProfile(profile);
    }

    private void DeleteProfile(ApiProfile profile)
    {
        if (!AppDialog.Confirm($"删除接口「{profile.Name}」？", owner: this))
            return;

        _working.ApiProfiles.Remove(profile);
        _working.AutoProviderIds.Remove(profile.Id);
        if (_working.Provider == profile.Id)
            _working.Provider = "auto";
        if (ReferenceEquals(_selectedProfile, profile))
            _selectedProfile = null;
        RefreshProfileList();
    }

    private void UpdateKeyHelpUi()
    {
        var kind = ProfileKindCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible;
        if (_selectedProfile is not null)
            ProfileTypeText.Text = ApiProfileDisplay.GetProviderBrandLabel(_selectedProfile);
    }

    private void OnOpenProviderHelp(object sender, RoutedEventArgs e)
    {
        var kind = ProfileKindCombo.SelectedValue as string ?? ApiProfileKinds.OpenAiCompatible;
        if (kind == ApiProfileKinds.OpenAiCompatible)
        {
            if (ProviderKeyHelp.TryDetectOpenAiLink(ProfileUrlBox.Text, ProfileNameBox.Text) is { } detected)
            {
                OpenKeyHelpUrl(detected.Url, detected.Hint);
                return;
            }

            if (FindResource("OpenAiKeyHelpMenu") is System.Windows.Controls.ContextMenu menu)
            {
                menu.PlacementTarget = KeyHelpButton;
                menu.IsOpen = true;
            }

            return;
        }

        var link = ProviderKeyHelp.GetPrimaryLink(kind, ProfileUrlBox.Text, ProfileNameBox.Text);
        OpenKeyHelpUrl(link.Url, link.Hint);
    }

    private void OnOpenKeyHelpLink(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: string url })
            OpenKeyHelpUrl(url, null);
    }

    private void OpenKeyHelpUrl(string url, string? hint)
    {
        try
        {
            ProviderKeyHelp.OpenUrl(url);
            if (!string.IsNullOrWhiteSpace(hint))
                KindHintText.Text = hint;
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"无法打开浏览器：{ex.Message}\n{url}", owner: this);
        }
    }

    private void OnOpenConfigDir(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(ConfigService.ConfigDirectory);
        Process.Start(new ProcessStartInfo(ConfigService.ConfigDirectory) { UseShellExecute = true });
    }

    private void OnOpenPrivacy(object sender, RoutedEventArgs e) => LegalDocuments.Open("privacy.md", this);

    private void OnOpenTerms(object sender, RoutedEventArgs e) => LegalDocuments.Open("terms.md", this);

    private void OnOpenComplianceMemo(object sender, RoutedEventArgs e) =>
        LegalDocuments.Open("compliance-ai-memo.md", this);

    private async void OnCheckUpdate(object sender, RoutedEventArgs e) =>
        await UpdateCheckService.CheckAndNotifyAsync(this).ConfigureAwait(true);

    private void OnExportHistory(object sender, RoutedEventArgs e)
    {
        if (EnableHistoryCheck.IsChecked != true)
        {
            AppDialog.Info("请先在下方勾选「保存翻译到本地历史」。", owner: this);
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
            if (dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                _historyStore.ExportToJsonFile(dialog.FileName);
            else
                _historyStore.ExportToTextFile(dialog.FileName);
            AppDialog.Info($"已导出到：{dialog.FileName}", owner: this);
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"导出失败：{ex.Message}", owner: this);
        }
    }

    private async void OnImportHistory(object sender, RoutedEventArgs e)
    {
        if (EnableHistoryCheck.IsChecked != true)
        {
            AppDialog.Info("请先在下方勾选「保存翻译到本地历史」。", owner: this);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "导入翻译历史",
            Filter = "文本或 JSON|*.txt;*.json|文本文件|*.txt|JSON 备份|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        ImportParseSummary summary;
        try
        {
            var path = dialog.FileName;
            summary = await System.Threading.Tasks.Task.Run(() => HistoryStore.AnalyzeImportFile(path)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"无法读取文件：{ex.Message}", owner: this);
            return;
        }

        if (summary.EntryCount == 0)
        {
            AppDialog.Warning(
                "未识别到有效条目。\n\n请使用炫译导出的 TXT/JSON，或确保格式为：\n原文\n\n译文\n\n\n\n（下一条）",
                owner: this);
            return;
        }

        var preview = HistoryTextFormat.BuildPreviewText(summary.Pairs);
        var mergeHint = summary.MaybeMergedSingleRecord
            ? "\n\n⚠ 文件里有多段空行，但只识别到 1 条——可能多条被合并。建议用「导出」生成的 TXT 再导入。"
            : "";
        if (!AppDialog.Confirm(
                $"识别到 {summary.EntryCount} 条记录，预览：\n\n{preview}{mergeHint}\n\n是否继续导入？",
                AppBranding.SettingsTitle,
                this))
            return;

        var mode = await AppDialog.AskImportHistoryModeAsync(AppBranding.SettingsTitle, this).ConfigureAwait(true);
        if (mode is null)
            return;

        var merge = mode == AppDialog.ImportHistoryMode.Merge;

        var importButton = sender as System.Windows.Controls.Button;
        if (importButton is not null)
            importButton.IsEnabled = false;

        try
        {
            var path = dialog.FileName;
            var count = await System.Threading.Tasks.Task.Run(() =>
            {
                using var store = new HistoryStore();
                return store.ImportFromFile(path, merge);
            }).ConfigureAwait(true);

            AppDialog.Info($"已导入 {count} 条记录。", owner: this);
            UpdateHistoryStatusUi();
            if (HistoryPanelWindow.IsPanelVisible)
                HistoryPanelWindow.Instance.ReloadHistory(new HistoryStore());
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"导入失败：{ex.Message}", owner: this);
        }
        finally
        {
            if (importButton is not null)
                importButton.IsEnabled = true;
        }
    }

    private void OnExportDiagnostics(object sender, RoutedEventArgs e) =>
        Services.DiagnosticExportService.ExportWithPrompt();

    private void OnClearHistory(object sender, RoutedEventArgs e)
    {
        if (!AppDialog.Confirm("确定清空本机全部翻译历史？", owner: this, icon: MessageBoxImage.Warning, dangerPrimary: true))
            return;
        _historyStore.Clear();
        AppDialog.Info("历史记录已清空。", owner: this);
        if (HistoryPanelWindow.IsPanelVisible)
            HistoryPanelWindow.Instance.ReloadHistory(new HistoryStore());
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        OnTranslationOpacityBoxLostFocus(this, e);
        OnHistoryOpacityBoxLostFocus(this, e);
        OnGeneralFontScaleBoxLostFocus(this, e);
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
        _working.ShowPanelOnTranslate = ShowPanelOnTranslateCheck.IsChecked == true;
        _working.ShowPanelOnHover = ShowPanelOnHoverCheck.IsChecked == true;
        _working.SuppressPanelAfterUserClose = SuppressPanelAfterCloseCheck.IsChecked == true;
        _working.HideSettingsForScreenshotPick = HideSettingsForScreenshotPickCheck.IsChecked == true;
        _working.CloseSettingsAfterSave = CloseAfterSaveCheck.IsChecked == true;
        _working.ShowStartupNotice = ShowStartupNoticeCheck.IsChecked == true;
        _working.TranslateOnSelection = TranslateOnSelectionCheck.IsChecked == true;
        _working.TranslateOnCopy = TranslateOnCopyCheck.IsChecked == true;
        _working.EnableScreenshotRegionHotkey = EnableScreenshotHotkeyCheck.IsChecked == true;
        var screenshotHotkey = ScreenshotHotkeyBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(screenshotHotkey))
            screenshotHotkey = "Ctrl+Shift+S";
        try
        {
            HotkeyParser.Parse(screenshotHotkey);
            _working.ScreenshotRegionHotkey = screenshotHotkey;
        }
        catch (Exception ex)
        {
            AppDialog.Warning($"截屏热键无效：{ex.Message}", owner: this);
            return;
        }

        if (TranslationDirectionCombo.SelectedValue is string dir)
            _working.TranslationDirection = dir;

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
            AppDialog.Warning($"热键无效：{ex.Message}", owner: this);
            return;
        }

        var globalTheme = GeneralThemeCombo.SelectedValue as string ?? "dark";
        var globalScale = ConfigService.ClampFontSizeScale(GeneralFontScaleSlider.Value / 100.0);
        var globalFont = GeneralFontFamilyCombo.SelectedValue as string ?? "";

        _working.TranslationPanelUi = new PanelChromeOptions
        {
            Opacity = PanelAppearance.FromPercent((int)TranslationOpacitySlider.Value),
            Theme = TranslationThemeCombo.SelectedValue as string ?? globalTheme,
            FontSizeScale = globalScale,
            FontFamilyName = globalFont,
            ShowExtras = TranslationExtrasCheck.IsChecked == true
        };
        _working.HistoryPanelUi = new PanelChromeOptions
        {
            Opacity = PanelAppearance.FromPercent((int)HistoryOpacitySlider.Value),
            Theme = HistoryThemeCombo.SelectedValue as string ?? globalTheme,
            FontSizeScale = globalScale,
            FontFamilyName = globalFont,
            ShowExtras = HistoryExtrasCheck.IsChecked == true
        };

        _configService.Save(_working);
        ApplyFloatingWindowSettings(_working);
        UpdateActiveProviderStatus();
        Saved?.Invoke(this, CloneConfig(_working));

        SaveHintText.Text = "已保存，配置已生效";
        SaveHintText.Visibility = Visibility.Visible;

        if (CloseAfterSaveCheck.IsChecked == true)
            Close();
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? source;
    }

    private static void ApplyFloatingWindowSettings(AppConfig config)
    {
        TranslationResultWindow.ApplyBehaviorFromConfig(config);
        TranslationResultWindow.Instance.ApplyAppearance(config.TranslationPanelUi);
        HistoryPanelWindow.Instance.ApplyAppearance(config.HistoryPanelUi);
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
        if (!_recordingHotkey && !_recordingScreenshotHotkey) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (!HotkeyCapture.TryFormat(Keyboard.Modifiers, key, out var hotkey))
            return;

        if (_recordingScreenshotHotkey)
        {
            ScreenshotHotkeyBox.Text = hotkey;
            _recordingScreenshotHotkey = false;
        }
        else
        {
            HotkeyBox.Text = hotkey;
            _recordingHotkey = false;
        }

        PreviewKeyDown -= OnHotkeyPreviewKeyDown;
        HotkeyHintText.Text = "录制：点击「按下录制」后，在键盘上按下组合键（需含 Ctrl/Shift/Alt 之一）";
    }

    private void OnRecordScreenshotHotkey(object sender, RoutedEventArgs e)
    {
        _recordingScreenshotHotkey = true;
        HotkeyHintText.Text = "正在录制截屏热键：请按下组合键（需含 Ctrl / Shift / Alt 之一）…";
        PreviewKeyDown += OnHotkeyPreviewKeyDown;
        Focus();
    }

    private void OnResetHotkey(object sender, RoutedEventArgs e) =>
        HotkeyBox.Text = HotkeyParser.DefaultHotkey;

    private void OnResetScreenshotHotkey(object sender, RoutedEventArgs e) =>
        ScreenshotHotkeyBox.Text = "Ctrl+Shift+S";

    private async void OnScreenshotRegionNow(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is not App app)
            return;

        var hideSettings = HideSettingsForScreenshotPickCheck.IsChecked == true;
        if (!hideSettings)
        {
            var ask = AppDialog.Confirm(
                "不隐藏设置窗口时，框选区域可能被设置窗遮挡。\n\n是否暂时隐藏设置窗口后再框选？",
                "框选截屏翻译",
                owner: this);
            if (!ask)
            {
                await app.TriggerScreenshotRegionTranslateAsync().ConfigureAwait(true);
                return;
            }

            hideSettings = true;
        }

        try
        {
            if (hideSettings)
                Hide();
            await app.TriggerScreenshotRegionTranslateAsync().ConfigureAwait(true);
        }
        finally
        {
            if (hideSettings)
            {
                Show();
                Activate();
            }
        }
    }

    private void SelectTranslationDirection(string? id)
    {
        var key = string.IsNullOrWhiteSpace(id)
            ? TranslationDirectionResolver.ModeAuto
            : id.Trim();
        TranslationDirectionCombo.SelectedValue = TranslationDirectionItems
            .Any(i => i.Id == key)
            ? key
            : TranslationDirectionResolver.ModeAuto;
    }

    private sealed record ThemeOption(string Id, string Label);
    private sealed record FontFamilyOption(string Id, string Label);
    private sealed record DirectionOption(string Id, string Label);
    private sealed record ProviderItem(string Id, string Label);
    private sealed record TemplateItem(string Kind, string Label);

    private sealed class ProfileListItemVm
    {
        public required ApiProfile Profile { get; init; }
        public required string PrimaryLabel { get; init; }
        public required string SecondaryLabel { get; init; }
    }
}
