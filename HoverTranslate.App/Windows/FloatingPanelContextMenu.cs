using System.Text;
using System.Windows.Controls;

namespace HoverTranslate.App.Windows;

internal static class FloatingPanelContextMenu
{
    public static void AttachCopyMenu(System.Windows.Controls.TextBox box, Func<string> getFullText)
    {
        var menu = new ContextMenu();
        var copy = new MenuItem { Header = "复制（译文+原文）" };
        copy.Click += (_, _) =>
        {
            var text = string.IsNullOrEmpty(box.SelectedText) ? getFullText() : box.SelectedText;
            if (!string.IsNullOrWhiteSpace(text))
                System.Windows.Clipboard.SetText(text);
        };
        var selectAll = new MenuItem { Header = "全选" };
        selectAll.Click += (_, _) => box.SelectAll();
        menu.Items.Add(copy);
        menu.Items.Add(selectAll);
        box.ContextMenu = menu;
    }

    public static void AttachHistoryListMenu(
        System.Windows.Controls.ListBox list,
        Func<string?> getSelectedCopyText,
        Func<string> getAllCopyText)
    {
        var menu = new ContextMenu();
        var copy = new MenuItem { Header = "复制（译文+原文）" };
        copy.Click += (_, _) =>
        {
            var text = getSelectedCopyText();
            if (!string.IsNullOrWhiteSpace(text))
                System.Windows.Clipboard.SetText(text);
        };
        var copyAll = new MenuItem { Header = "复制全部记录" };
        copyAll.Click += (_, _) =>
        {
            var text = getAllCopyText();
            if (!string.IsNullOrWhiteSpace(text))
                System.Windows.Clipboard.SetText(text);
        };
        menu.Items.Add(copy);
        menu.Items.Add(copyAll);
        list.ContextMenu = menu;
    }
}
