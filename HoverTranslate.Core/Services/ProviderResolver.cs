using HoverTranslate.Core.Models;
using HoverTranslate.Core.Translators;

namespace HoverTranslate.Core.Services;

public static class ProviderResolver
{
    public static bool HasApiKey(AppConfig config)
    {
        if (GetEnabledProfiles(config).Any(p => !string.IsNullOrWhiteSpace(p.ApiKey)))
            return true;
        // 兼容尚未写入 apiProfiles 的旧配置
        return !string.IsNullOrWhiteSpace(config.Bailian.ApiKey)
               || !string.IsNullOrWhiteSpace(config.Deepseek.ApiKey);
    }

    public static ApiProfile? GetActiveProfile(AppConfig config)
    {
        if (string.Equals(config.Provider, "auto", StringComparison.OrdinalIgnoreCase))
            return GetEnabledProfiles(config).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ApiKey));

        return config.ApiProfiles.FirstOrDefault(p =>
            p.Id == config.Provider && p.Enabled && !string.IsNullOrWhiteSpace(p.ApiKey));
    }

    public static string ResolveDisplayName(AppConfig config)
    {
        if (string.Equals(config.Provider, "auto", StringComparison.OrdinalIgnoreCase))
            return "Auto";

        return config.ApiProfiles.FirstOrDefault(p => p.Id == config.Provider)?.Name ?? "未配置";
    }

    public static List<AutoProviderEntry> ListAllProviders(AppConfig config) =>
        GetEnabledProfiles(config)
            .Select(p => new AutoProviderEntry(p.Id, $"{p.Name} ({KindLabel(p.Kind)})", !string.IsNullOrWhiteSpace(p.ApiKey)))
            .ToList();

    public static List<ApiProfile> GetAutoCandidates(AppConfig config)
    {
        var all = GetEnabledProfiles(config).Where(p => !string.IsNullOrWhiteSpace(p.ApiKey)).ToList();
        if (config.AutoProviderIds.Count == 0)
            return all;

        var ordered = new List<ApiProfile>();
        foreach (var id in config.AutoProviderIds)
        {
            var hit = all.FirstOrDefault(p => p.Id == id);
            if (hit is not null)
                ordered.Add(hit);
        }

        return ordered;
    }

    public static IEnumerable<ApiProfile> GetEnabledProfiles(AppConfig config) =>
        config.ApiProfiles.Where(p => p.Enabled);

    public static ITranslator CreateTranslator(AppConfig config, GlossaryService glossary)
    {
        if (string.Equals(config.Provider, "auto", StringComparison.OrdinalIgnoreCase))
            return new AutoTranslator(config, glossary);

        var profile = config.ApiProfiles.FirstOrDefault(p => p.Id == config.Provider);
        if (profile is null)
            throw new InvalidOperationException("请先在设置中添加并选择翻译接口。");

        return ProfileTranslatorFactory.Create(profile, config, glossary);
    }

    public static LlmProviderConfig ToLlmConfig(ApiProfile profile) => new()
    {
        ApiKey = profile.ApiKey,
        BaseUrl = profile.BaseUrl,
        Model = profile.Model
    };

    public static string KindLabel(string? kind) => kind?.ToLowerInvariant() switch
    {
        ApiProfileKinds.Microsoft => "微软",
        ApiProfileKinds.Google => "谷歌",
        _ => "OpenAI兼容"
    };
}

public sealed record AutoProviderEntry(string Id, string Label, bool HasKey);
