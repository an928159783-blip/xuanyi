using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;

namespace HoverTranslate.App.Windows;

public enum AppDialogKind
{
    Default,
    ImportHistory
}

public partial class AppDialogWindow : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public AppDialogWindow(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon,
        bool dangerPrimary = false,
        AppDialogKind kind = AppDialogKind.Default)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ApplyIcon(icon);
        BuildButtons(buttons, dangerPrimary, kind);
        DragHeader.MouseLeftButtonDown += OnDragHeaderMouseDown;
        Loaded += (_, _) =>
        {
            var primary = ButtonPanel.Children.OfType<WpfButton>().LastOrDefault();
            primary?.Focus();
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Result = MessageBoxResult.Cancel;
                Close();
            }
        };
    }

    private void OnDragHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
            return;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            try { DragMove(); }
            catch { /* ignore */ }
        }
    }

    private void ApplyIcon(MessageBoxImage icon)
    {
        switch (icon)
        {
            case MessageBoxImage.Warning:
                IconHost.Background = new SolidColorBrush(WpfColor.FromRgb(0xFF, 0xFB, 0xEB));
                IconGlyph.Text = "!";
                IconGlyph.Foreground = new SolidColorBrush(WpfColor.FromRgb(0xD9, 0x77, 0x06));
                break;
            case MessageBoxImage.Error:
                IconHost.Background = new SolidColorBrush(WpfColor.FromRgb(0xFE, 0xF2, 0xF2));
                IconGlyph.Text = "×";
                IconGlyph.Foreground = new SolidColorBrush(WpfColor.FromRgb(0xDC, 0x26, 0x26));
                break;
            case MessageBoxImage.Question:
                IconHost.Background = new SolidColorBrush(WpfColor.FromRgb(0xEF, 0xF6, 0xFF));
                IconGlyph.Text = "?";
                IconGlyph.Foreground = new SolidColorBrush(WpfColor.FromRgb(0x25, 0x63, 0xEB));
                break;
            case MessageBoxImage.Information:
                IconHost.Background = new SolidColorBrush(WpfColor.FromRgb(0xEF, 0xF6, 0xFF));
                IconGlyph.Text = "i";
                IconGlyph.Foreground = new SolidColorBrush(WpfColor.FromRgb(0x25, 0x63, 0xEB));
                break;
            default:
                IconHost.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void BuildButtons(MessageBoxButton buttons, bool dangerPrimary, AppDialogKind kind)
    {
        if (kind == AppDialogKind.ImportHistory)
        {
            AddButton("取消", MessageBoxResult.Cancel, isPrimary: false);
            AddButton("清空后导入", MessageBoxResult.No, isPrimary: false);
            AddButton("追加", MessageBoxResult.Yes, isPrimary: true);
            return;
        }

        switch (buttons)
        {
            case MessageBoxButton.YesNo:
                AddButton("否", MessageBoxResult.No, isPrimary: false);
                AddButton("是", MessageBoxResult.Yes, isPrimary: true, danger: dangerPrimary);
                break;
            case MessageBoxButton.OKCancel:
                AddButton("取消", MessageBoxResult.Cancel, isPrimary: false);
                AddButton("确定", MessageBoxResult.OK, isPrimary: true);
                break;
            default:
                AddButton("确定", MessageBoxResult.OK, isPrimary: true);
                break;
        }
    }

    private void AddButton(string label, MessageBoxResult result, bool isPrimary, bool danger = false)
    {
        var btn = new WpfButton
        {
            Content = label,
            MinWidth = 80,
            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(8, 0, 0, 0)
        };
        if (danger && isPrimary)
            btn.Style = (Style)FindResource("DangerButton");
        else if (isPrimary)
            btn.Style = (Style)FindResource("PrimaryButton");

        btn.Click += (_, _) =>
        {
            Result = result;
            Close();
        };
        ButtonPanel.Children.Add(btn);
    }
}
