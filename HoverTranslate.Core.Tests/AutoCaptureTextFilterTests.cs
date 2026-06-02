using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;
using Xunit;

namespace HoverTranslate.Core.Tests;

public class AutoCaptureTextFilterTests
{
    [Fact]
    public void PickTranslatable_MultilineMixed_KeepsEnglishForEnToZh()
    {
        var config = new AppConfig { TranslationDirection = TranslationDirectionResolver.ModeEnToZh };
        var text = "好的办法发\nAdd agents, context, tools";
        var picked = AutoCaptureTextFilter.PickTranslatable(text, config);
        Assert.Equal("Add agents, context, tools", picked);
    }

    [Fact]
    public void AddPresets_OnlyThreeOptions()
    {
        Assert.Equal(3, ApiProfileAddPresets.All.Count);
        Assert.Contains(ApiProfileAddPresets.All, p => p.Id == "bailian");
        Assert.Contains(ApiProfileAddPresets.All, p => p.Id == "deepseek");
        Assert.Contains(ApiProfileAddPresets.All, p => p.Id == "custom");
    }
}
