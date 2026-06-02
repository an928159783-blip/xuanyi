using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

using HoverTranslate.App.Services;

namespace HoverTranslate.App.Windows;

public partial class OverlayWindow : System.Windows.Window
{
    private static OverlayWindow? _instance;
    private System.Windows.Threading.DispatcherTimer? _autoCloseTimer;

    private OverlayWindow()
    {
        InitializeComponent();
    }

    public static void HideActive()
    {
        if (_instance is null) return;
        _instance._autoCloseTimer?.Stop();
        _instance.Hide();
    }

    public static void ShowResult(TranslationResult result, AppConfig? config = null)
    {
        config ??= new ConfigService().Load();
        var win = _instance ??= new OverlayWindow();
        var scale = ConfigService.ClampFontSizeScale(config.TranslationPanelUi.FontSizeScale);
        win.ResultText.FontSize = Math.Round(13.0 * scale, 1);
        win.ResultText.FontFamily = PanelAppearance.ResolveFontFamily(config.TranslationPanelUi.FontFamilyName);

        if (result.Success)
        {
            win.TitleText.Text = $"译文 ({result.Provider})";
            win.ResultText.Text = result.TranslatedText;
        }
        else
        {
            win.TitleText.Text = "翻译失败";
            win.ResultText.Text = result.ErrorMessage ?? "未知错误";
        }

        var workArea = System.Windows.SystemParameters.WorkArea;
        win.Left = workArea.Right - win.Width - 24;
        win.Top = workArea.Bottom - win.Height - 24;
        win.Show();
        win.Activate();

        win._autoCloseTimer?.Stop();
        if (config.OverlayTimeoutMs > 0)
        {
            win._autoCloseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(config.OverlayTimeoutMs)
            };
            win._autoCloseTimer.Tick += (_, _) =>
            {
                win._autoCloseTimer?.Stop();
                win.Hide();
            };
            win._autoCloseTimer.Start();
        }
    }

    private void OnCopy(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ResultText.Text))
            ClipboardGuard.SetText(ResultText.Text);
    }

    private void OnClose(object sender, System.Windows.RoutedEventArgs e) => Hide();
}
