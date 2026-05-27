using Xunit;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Tests;

public class TranslationDirectionResolverTests
{
    [Fact]
    public void ResolveApiPair_auto_english_to_chinese()
    {
        var (from, to) = TranslationDirectionResolver.ResolveApiPair(
            "Optical Character Recognition", new AppConfig());
        Assert.Equal("en", from);
        Assert.Equal("zh-Hans", to);
    }

    [Fact]
    public void ResolveApiPair_auto_chinese_to_english()
    {
        var (from, to) = TranslationDirectionResolver.ResolveApiPair(
            "这是中文测试内容", new AppConfig());
        Assert.Equal("zh-Hans", from);
        Assert.Equal("en", to);
    }

    [Fact]
    public void ResolveApiPair_fixed_zh_to_en()
    {
        var config = new AppConfig { TranslationDirection = "zh-to-en" };
        var (from, to) = TranslationDirectionResolver.ResolveApiPair("Hello", config);
        Assert.Equal("zh-Hans", from);
        Assert.Equal("en", to);
    }
}
