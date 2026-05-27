using Xunit;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Translators;
using RichardSzalay.MockHttp;

namespace HoverTranslate.Core.Tests;

public class MicrosoftTranslatorIntegrationTests
{
    [Fact]
    public async Task TranslateAsync_success_parses_response()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.cognitive.microsofttranslator.com/translate*")
            .Respond("application/json",
                """[{"translations":[{"text":"你好，世界"}]}]""");

        var profile = new ApiProfile
        {
            Id = "ms-test",
            Name = "测试微软",
            Kind = ApiProfileKinds.Microsoft,
            ApiKey = "test-key",
            Region = "global"
        };

        var config = new AppConfig();
        var translator = new MicrosoftTranslator(profile, config, mockHttp.ToHttpClient());
        var result = await translator.TranslateAsync("Hello world");

        Assert.True(result.Success);
        Assert.Equal("你好，世界", result.TranslatedText);
        Assert.Equal("测试微软", result.Provider);
    }

    [Fact]
    public async Task TranslateAsync_http_401_returns_failure()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.cognitive.microsofttranslator.com/translate*")
            .Respond(System.Net.HttpStatusCode.Unauthorized, "application/json", """{"error":{"message":"invalid"}}""");

        var profile = new ApiProfile
        {
            Id = "ms-test",
            Name = "测试微软",
            Kind = ApiProfileKinds.Microsoft,
            ApiKey = "bad-key",
            Region = "global"
        };

        var config = new AppConfig();
        var translator = new MicrosoftTranslator(profile, config, mockHttp.ToHttpClient());
        var result = await translator.TranslateAsync("Hello");

        Assert.False(result.Success);
        Assert.Contains("401", result.ErrorMessage ?? "", StringComparison.Ordinal);
    }
}
