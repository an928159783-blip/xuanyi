using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;
using HoverTranslate.Core.Translators;
using Xunit;

namespace HoverTranslate.Core.Tests;

public class TranslatorFactoryTests
{
    [Fact]
    public void ProfileFactory_Microsoft_ReturnsMicrosoftTranslator()
    {
        var profile = new ApiProfile { Kind = ApiProfileKinds.Microsoft, Name = "MS", ApiKey = "k" };
        var t = ProfileTranslatorFactory.Create(profile, new AppConfig(), new GlossaryService());
        Assert.IsType<MicrosoftTranslator>(t);
    }

    [Fact]
    public void ProfileFactory_Google_ReturnsGoogleTranslator()
    {
        var profile = new ApiProfile { Kind = ApiProfileKinds.Google, Name = "G", ApiKey = "k" };
        var t = ProfileTranslatorFactory.Create(profile, new AppConfig(), new GlossaryService());
        Assert.IsType<GoogleTranslator>(t);
    }

    [Fact]
    public void ProfileFactory_OpenAi_ReturnsGenericLlm()
    {
        var profile = new ApiProfile
        {
            Kind = ApiProfileKinds.OpenAiCompatible,
            Name = "DS",
            ApiKey = "k",
            BaseUrl = "https://api.deepseek.com",
            Model = "deepseek-chat"
        };
        var t = ProfileTranslatorFactory.Create(profile, new AppConfig(), new GlossaryService());
        Assert.IsType<GenericLlmTranslator>(t);
    }

    [Fact]
    public void AutoTranslator_ProviderName_IsAuto()
    {
        var t = new AutoTranslator(new AppConfig(), new GlossaryService());
        Assert.Equal("auto", t.ProviderName);
    }
}
