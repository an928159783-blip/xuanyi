using HoverTranslate.Core.Services;
using Xunit;

namespace HoverTranslate.Core.Tests;

public sealed class HistoryTextFormatTests
{
    [Fact]
    public void Export_and_parse_roundtrip()
    {
        var exported = HistoryTextFormat.FormatExportBlock("Hello", "你好");
        exported += HistoryTextFormat.EntryDelimiter;
        exported += HistoryTextFormat.FormatExportBlock("中文", "English");

        var pairs = HistoryTextFormat.ParseImport(exported);
        Assert.Equal(2, pairs.Count);
        Assert.Equal("Hello", pairs[0].Source);
        Assert.Equal("你好", pairs[0].Target);
        Assert.Equal("中文", pairs[1].Source);
        Assert.Equal("English", pairs[1].Target);
    }

    [Fact]
    public void Export_has_no_metadata_labels()
    {
        var text = HistoryTextFormat.FormatExportBlock("A", "B");
        Assert.DoesNotContain("译文", text);
        Assert.DoesNotContain("原文", text);
        Assert.Equal("A\n\nB", text);
    }

    [Fact]
    public void Parses_blocks_separated_by_triple_newline()
    {
        const string text = "问题一\n\nQuestion one\n\n\n问题二\n\nQuestion two";
        var pairs = HistoryTextFormat.ParseImport(text);
        Assert.Equal(2, pairs.Count);
        Assert.Equal("问题一", pairs[0].Source);
        Assert.Equal("Question one", pairs[0].Target);
    }
}
