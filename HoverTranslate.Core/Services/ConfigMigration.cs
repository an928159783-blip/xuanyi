using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>将旧版内置 DeepSeek/百炼配置迁移为 ApiProfiles，不再在 UI 中展示内置通道。</summary>
public static class ConfigMigration
{
    public static void MigrateBuiltInToProfiles(AppConfig config)
    {
        var hasProfiles = config.ApiProfiles.Count > 0;

        if (!hasProfiles && !string.IsNullOrWhiteSpace(config.Deepseek.ApiKey))
        {
            config.ApiProfiles.Add(new ApiProfile
            {
                Name = "DeepSeek",
                Kind = ApiProfileKinds.OpenAiCompatible,
                ApiKey = config.Deepseek.ApiKey,
                BaseUrl = string.IsNullOrWhiteSpace(config.Deepseek.BaseUrl)
                    ? "https://api.deepseek.com"
                    : config.Deepseek.BaseUrl,
                Model = string.IsNullOrWhiteSpace(config.Deepseek.Model)
                    ? "deepseek-chat"
                    : config.Deepseek.Model
            });
        }

        if (!hasProfiles && !string.IsNullOrWhiteSpace(config.Bailian.ApiKey))
        {
            config.ApiProfiles.Add(new ApiProfile
            {
                Name = "阿里云百炼",
                Kind = ApiProfileKinds.OpenAiCompatible,
                ApiKey = config.Bailian.ApiKey,
                BaseUrl = string.IsNullOrWhiteSpace(config.Bailian.BaseUrl)
                    ? "https://dashscope.aliyuncs.com/compatible-mode/v1"
                    : config.Bailian.BaseUrl,
                Model = string.IsNullOrWhiteSpace(config.Bailian.Model)
                    ? "qwen-turbo"
                    : config.Bailian.Model
            });
        }

        if (string.Equals(config.Provider, "deepseek", StringComparison.OrdinalIgnoreCase)
            || string.Equals(config.Provider, "bailian", StringComparison.OrdinalIgnoreCase))
        {
            var migrated = config.ApiProfiles.FirstOrDefault();
            config.Provider = migrated?.Id ?? "auto";
        }

        if (string.IsNullOrWhiteSpace(config.Provider) && config.ApiProfiles.Count > 0)
            config.Provider = "auto";
    }
}
