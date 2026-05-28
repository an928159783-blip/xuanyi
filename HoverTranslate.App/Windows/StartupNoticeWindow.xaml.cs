using System.Windows;

namespace HoverTranslate.App.Windows;

public partial class StartupNoticeWindow : Window
{
    public bool DontShowAgain { get; private set; }

    public StartupNoticeWindow()
    {
        InitializeComponent();
        BuildStampText.Text = HoverTranslate.App.Services.AppBuildInfo.FormatForDisplay();
    }

    private void OnShowFeatureGuide(object sender, RoutedEventArgs e)
    {
        SettingsWindowHost.ShowFeatureGuide(this);
        DontShowAgain = DontShowAgainCheck.IsChecked == true;
        Close();
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        SettingsWindowHost.ShowOrActivate(navIndex: 4);
        DontShowAgain = DontShowAgainCheck.IsChecked == true;
        Close();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DontShowAgain = DontShowAgainCheck.IsChecked == true;
        Close();
    }
}
