using System.Windows;

namespace HoverTranslate.App.Windows;

public partial class StartupNoticeWindow : Window
{
    public bool DontShowAgain { get; private set; }

    public StartupNoticeWindow()
    {
        InitializeComponent();
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        SettingsWindowHost.ShowOrActivate();
        DontShowAgain = DontShowAgainCheck.IsChecked == true;
        Close();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DontShowAgain = DontShowAgainCheck.IsChecked == true;
        Close();
    }
}
