using System.Text;
using System.Text.RegularExpressions;
using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>历史导出/导入的纯文本格式：每条「原文 + 空行 + 译文」，条与条之间至少三行空行分隔。</summary>
public static class HistoryTextFormat
{
    /// <summary>条与条之间：三行空行（四条换行）。</summary>
    public const string EntryDelimiter = "\n\n\n\n";

    private static readonly Regex BlockSplitRegex = new(@"\n{3,}", RegexOptions.Compiled);

    public static string FormatExportBlock(string source, string target)
    {
        source = source?.Trim() ?? "";
        target = target?.Trim() ?? "";
        if (string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(source))
            target = source;
        if (string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
            source = target;
        if (string.IsNullOrEmpty(source))
            return "";

        if (string.Equals(source, target, StringComparison.Ordinal))
            return source;

        return $"{source}\n\n{target}";
    }

    public static string FormatExport(IReadOnlyList<HistoryEntry> entries)
    {
        var blocks = entries
            .Select(e => FormatExportBlock(e.Source, e.Target))
            .Where(b => !string.IsNullOrWhiteSpace(b));
        return string.Join(EntryDelimiter, blocks);
    }

    public static List<(string Source, string Target)> ParseImport(string text)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var blocks = BlockSplitRegex.Split(normalized);
        if (blocks.Length == 1 && normalized.Contains(EntryDelimiter, StringComparison.Ordinal))
            blocks = normalized.Split(EntryDelimiter, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var pair = ParseBlock(block);
            if (pair is not null)
                result.Add(pair.Value);
        }

        return result;
    }

    public static ImportParseSummary SummarizeImport(string text)
    {
        var pairs = ParseImport(text);
        var normalized = text?.Replace("\r\n", "\n").Replace('\r', '\n').Trim() ?? "";
        var blankGapCount = Regex.Matches(normalized, @"\n\s*\n").Count;
        var maybeMerged = pairs.Count <= 1 && blankGapCount >= 3 && normalized.Length > 80;

        return new ImportParseSummary
        {
            EntryCount = pairs.Count,
            Pairs = pairs,
            MaybeMergedSingleRecord = maybeMerged
        };
    }

    public static string BuildPreviewText(IReadOnlyList<(string Source, string Target)> pairs, int maxEntries = 3, int maxChars = 120)
    {
        if (pairs.Count == 0)
            return "（未识别到有效条目）";

        var sb = new StringBuilder();
        var show = Math.Min(pairs.Count, maxEntries);
        for (var i = 0; i < show; i++)
        {
            if (i > 0) sb.AppendLine();
            sb.Append($"[{i + 1}] ");
            sb.Append(Truncate(pairs[i].Source, maxChars));
            if (!string.Equals(pairs[i].Source, pairs[i].Target, StringComparison.Ordinal))
            {
                sb.Append("  →  ");
                sb.Append(Truncate(pairs[i].Target, maxChars));
            }
        }

        if (pairs.Count > show)
            sb.AppendLine().Append($"... 另有 {pairs.Count - show} 条");
        return sb.ToString();
    }

    private static (string Source, string Target)? ParseBlock(string block)
    {
        var trimmed = block.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        var splitAt = trimmed.IndexOf("\n\n", StringComparison.Ordinal);
        if (splitAt < 0)
        {
            if (trimmed.Length < 2)
                return null;
            return (trimmed, trimmed);
        }

        var source = trimmed[..splitAt].Trim();
        var target = trimmed[(splitAt + 2)..].Trim();
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return null;
        if (string.IsNullOrEmpty(target))
            target = source;
        if (string.IsNullOrEmpty(source))
            source = target;
        if (source.Length < 1 && target.Length < 1)
            return null;

        return (source, target);
    }

    private static string Truncate(string text, int max)
    {
        text = text.Replace('\n', ' ').Trim();
        if (text.Length <= max) return text;
        return text[..max] + "…";
    }
}

public sealed class ImportParseSummary
{
    public int EntryCount { get; init; }
    public List<(string Source, string Target)> Pairs { get; init; } = [];
    public bool MaybeMergedSingleRecord { get; init; }
}
