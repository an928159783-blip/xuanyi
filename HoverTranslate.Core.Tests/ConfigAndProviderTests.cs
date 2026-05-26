using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;
using Xunit;

namespace HoverTranslate.Core.Tests;

public class ConfigAndProviderTests
{
    [Fact]
    public void MigrateBuiltIn_BailianKey_CreatesApiProfile()
    {
        var config = new AppConfig
        {
            Provider = "bailian",
            Bailian = new LlmProviderConfig
            {
                ApiKey = "sk-test-key",
                BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                Model = "qwen-turbo"
            }
        };

        ConfigMigration.MigrateBuiltInToProfiles(config);

        Assert.Single(config.ApiProfiles);
        Assert.Equal(ApiProfileKinds.OpenAiCompatible, config.ApiProfiles[0].Kind);
        Assert.NotEqual("bailian", config.Provider);
        Assert.True(ProviderResolver.HasApiKey(config));
    }

    [Fact]
    public void HasApiKey_WorksWithProfilesOnly()
    {
        var config = new AppConfig
        {
            ApiProfiles =
            [
                new ApiProfile { Kind = ApiProfileKinds.Google, ApiKey = "g-key", Enabled = true }
            ]
        };
        Assert.True(ProviderResolver.HasApiKey(config));
    }

    [Fact]
    public void GetAutoCandidates_RespectsOrder()
    {
        var a = new ApiProfile { Id = "a", Name = "A", ApiKey = "1", Enabled = true };
        var b = new ApiProfile { Id = "b", Name = "B", ApiKey = "2", Enabled = true };
        var config = new AppConfig
        {
            ApiProfiles = [a, b],
            AutoProviderIds = ["b", "a"]
        };
        var list = ProviderResolver.GetAutoCandidates(config);
        Assert.Equal("b", list[0].Id);
        Assert.Equal("a", list[1].Id);
    }

    [Fact]
    public void ListAllProviders_IncludesKindLabel()
    {
        var config = new AppConfig
        {
            ApiProfiles =
            [
                new ApiProfile { Id = "m1", Name = "MS", Kind = ApiProfileKinds.Microsoft, ApiKey = "k", Enabled = true }
            ]
        };
        var entries = ProviderResolver.ListAllProviders(config);
        Assert.Contains("微软", entries[0].Label);
    }

    [Fact]
    public void MigratePanelUi_ClampsOpacity()
    {
        var config = new AppConfig
        {
            TranslationPanelUi = { Opacity = 1.5 },
            HistoryPanelUi = { Opacity = -0.1 }
        };
        ConfigService.MigratePanelUi(config);
        Assert.Equal(1.0, config.TranslationPanelUi.Opacity);
        Assert.Equal(0.0, config.HistoryPanelUi.Opacity);
    }
}
