using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace HoverTranslate.App.Services;

public static class UiAutomationSelectionCapture
{
    public static bool HasActiveSelection()
    {
        var text = TryGetSelectedText();
        return IsUseful(text);
    }

    public static string? TryGetSelectedText()
    {
        try
        {
            var fromFocused = ReadFromFocusedElement();
            if (IsUseful(fromFocused))
                return fromFocused;

            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            var root = AutomationElement.FromHandle(hwnd);
            if (root is null || UiAutomationAppScope.IsFromThisProcess(root))
                return null;

            var fromRoot = ReadFromElement(root);
            if (IsUseful(fromRoot))
                return fromRoot;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? ReadFromFocusedElement()
    {
        var focused = AutomationElement.FocusedElement;
        if (focused is null || UiAutomationAppScope.IsFromThisProcess(focused))
            return null;

        return ReadFromElement(focused);
    }

    private static string? ReadFromElement(AutomationElement element)
    {
        var text = ReadTextPatternSelection(element);
        if (IsUseful(text))
            return text;

        return WalkChildrenForSelection(element, maxDepth: 5);
    }

    private static string? ReadTextPatternSelection(AutomationElement element)
    {
        try
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj)
                || patternObj is not TextPattern textPattern)
                return null;

            var ranges = textPattern.GetSelection();
            if (ranges == null || ranges.Length == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var range in ranges)
            {
                var part = range.GetText(-1);
                if (!string.IsNullOrWhiteSpace(part))
                    sb.Append(part);
            }

            var result = sb.ToString().Trim();
            return result.Length == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }

    private static string? WalkChildrenForSelection(AutomationElement parent, int maxDepth)
    {
        if (maxDepth <= 0) return null;

        try
        {
            var children = parent.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
            foreach (AutomationElement child in children)
            {
                var text = ReadTextPatternSelection(child);
                if (IsUseful(text))
                    return text;

                text = WalkChildrenForSelection(child, maxDepth - 1);
                if (IsUseful(text))
                    return text;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static bool IsUseful(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= 1;
}
