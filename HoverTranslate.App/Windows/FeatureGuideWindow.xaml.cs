using System.Windows;

namespace HoverTranslate.App.Windows;

public partial class FeatureGuideWindow : Window
{
    public FeatureGuideWindow()
    {
        InitializeComponent();
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        SettingsWindowHost.ShowOrActivate();
        Close();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
