using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Windows;

public partial class OverlayWindow : System.Windows.Window
{
    private OverlayWindow()
    {
        InitializeComponent();
    }

    public static void ShowResult(TranslationResult result)
    {
        var win = new OverlayWindow();
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
    }

    private void OnCopy(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ResultText.Text))
            Clipboard.SetText(ResultText.Text);
    }

    private void OnClose(object sender, System.Windows.RoutedEventArgs e) => Close();
}
