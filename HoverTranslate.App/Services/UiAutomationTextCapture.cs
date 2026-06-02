using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public static class UiAutomationTextCapture
{
    public static string? GetTextAtScreenPoint(int x, int y)
    {
        try
        {
            var point = new System.Windows.Point(x, y);
            var element = AutomationElement.FromPoint(point);
            if (element == null || UiAutomationAppScope.IsFromThisProcess(element))
                return null;

            var fromRange = TryGetTextFromRangeAtPoint(element, point);
            if (!string.IsNullOrWhiteSpace(fromRange))
                return fromRange.Trim();

            return TryGetTextFromElementTree(element);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetTextFromRangeAtPoint(AutomationElement element, System.Windows.Point point)
    {
        for (var current = element; current != null; current = GetParent(current))
        {
            if (!current.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj)
                || patternObj is not TextPattern textPattern)
                continue;

            try
            {
                var range = textPattern.RangeFromPoint(point);
                if (range == null) continue;

                // 优先连续段落/整行，再回退单词
                var paragraphText = ExpandRangeText(range, TextUnit.Paragraph);
                if (TryPickHoverText(paragraphText, out var fromParagraph))
                    return fromParagraph;

                var lineText = ExpandRangeText(range, TextUnit.Line);
                if (TryPickHoverText(lineText, out var fromLine))
                    return fromLine;

                var wordText = ExpandRangeText(range, TextUnit.Word);
                if (TextHeuristics.IsEnglishSnippet(wordText))
                    return wordText;

                if (TryPickHoverText(lineText, out var fallback))
                    return fallback;
            }
            catch
            {
                // RangeFromPoint 在部分 Electron/WebView 上会失败
            }
        }

        return null;
    }

    private static string ExpandRangeText(TextPatternRange range, TextUnit unit)
    {
        var chunk = range.Clone();
        chunk.ExpandToEnclosingUnit(unit);
        return chunk.GetText(-1)?.Trim() ?? "";
    }

    private static bool TryPickHoverText(string? text, out string result)
    {
        result = "";
        if (string.IsNullOrWhiteSpace(text)) return false;

        text = text.Trim();
        if (text.Length > 1200)
            text = text[..1200];

        if (TextHeuristics.ShouldTranslateOnHover(text))
        {
            result = text;
            return true;
        }

        var resolved = TextHeuristics.ResolveHoverTarget(text);
        if (string.IsNullOrWhiteSpace(resolved)) return false;

        if (resolved.Length < 3) return false;
        if (text.Length >= 12 && resolved.Length < Math.Min(12, text.Length / 4))
            return false;

        result = resolved;
        return true;
    }

    private static string? TryGetTextFromElementTree(AutomationElement element)
    {
        var parts = new List<string>();
        CollectFromElement(element, parts, depth: 0);

        var parent = TreeWalker.ControlViewWalker.GetParent(element);
        if (parent != null)
            CollectFromElement(parent, parts, depth: 0);

        var text = string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
            .Trim();
        return string.IsNullOrWhiteSpace(text) ? null : TextHeuristics.ResolveHoverTarget(text);
    }

    private static AutomationElement? GetParent(AutomationElement element)
    {
        try
        {
            return TreeWalker.ControlViewWalker.GetParent(element);
        }
        catch
        {
            return null;
        }
    }

    private static void CollectFromElement(AutomationElement element, List<string> parts, int depth)
    {
        if (depth > 3) return;

        try
        {
            if (element.Current.IsPassword) return;

            var name = element.Current.Name;
            if (!string.IsNullOrWhiteSpace(name) && name.Length <= 500)
                parts.Add(name.Trim());

            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObj)
                && valuePatternObj is ValuePattern valuePattern)
            {
                var v = valuePattern.Current.Value;
                if (!string.IsNullOrWhiteSpace(v) && v.Length <= 500)
                    parts.Add(v.Trim());
            }
        }
        catch
        {
            // ignore
        }
    }
}
