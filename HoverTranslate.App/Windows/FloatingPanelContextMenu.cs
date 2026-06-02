using System.Text;
using System.Windows.Controls;

using HoverTranslate.App.Services;

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
                ClipboardGuard.SetText(text);
        };
        var selectAll = new MenuItem { Header = "全选" };
        selectAll.Click += (_, _) => box.SelectAll();
        menu.Items.Add(copy);
        menu.Items.Add(selectAll);
        box.ContextMenu = menu;
    }
}
