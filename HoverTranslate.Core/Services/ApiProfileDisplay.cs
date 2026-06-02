using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>接口列表/通道下拉的人类可读标签（优先显示模型名，而非「OpenAI 兼容」）。</summary>
public static class ApiProfileDisplay
{
    public static string GetPrimaryLabel(ApiProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Model))
            return profile.Model.Trim();

        if (!string.IsNullOrWhiteSpace(profile.Name) && !IsGenericName(profile.Name))
            return profile.Name.Trim();

        return GetProviderBrandLabel(profile);
    }

    public static string GetSecondaryLabel(ApiProfile profile)
    {
        var brand = GetProviderBrandLabel(profile);
        if (profile.Kind == ApiProfileKinds.OpenAiCompatible
            && !string.IsNullOrWhiteSpace(profile.BaseUrl))
        {
            var host = TryGetHost(profile.BaseUrl);
            if (host is not null && !brand.Contains(host, StringComparison.OrdinalIgnoreCase))
                return $"{brand} · {host}";
        }

        return brand;
    }

    public static string GetProviderBrandLabel(ApiProfile profile) =>
        profile.Kind?.ToLowerInvariant() switch
        {
            ApiProfileKinds.Microsoft => "微软 Azure 翻译",
            ApiProfileKinds.Google => "Google Cloud 翻译",
            _ => ResolveOpenAiBrand(profile.BaseUrl, profile.Name, profile.Model)
        };

    public static string GetChannelLabel(ApiProfile profile) => GetPrimaryLabel(profile);

    public static string FormatActiveChannel(AppConfig config)
    {
        if (string.Equals(config.Provider, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var candidates = ProviderResolver.GetAutoCandidates(config);
            if (candidates.Count == 0)
                return "Auto（尚未配置可用接口）";
            var first = candidates[0];
            return $"Auto → 优先 {GetPrimaryLabel(first)}（{GetProviderBrandLabel(first)}）";
        }

        var profile = config.ApiProfiles.FirstOrDefault(p => p.Id == config.Provider);
        return profile is null
            ? "未选择接口"
            : $"{GetPrimaryLabel(profile)}（{GetProviderBrandLabel(profile)}）";
    }

    public static string SuggestNameFromModel(string? model, string kind)
    {
        if (!string.IsNullOrWhiteSpace(model))
            return model.Trim();
        return kind switch
        {
            ApiProfileKinds.Microsoft => "微软翻译",
            ApiProfileKinds.Google => "谷歌翻译",
            _ => "deepseek-chat"
        };
    }

    private static string ResolveOpenAiBrand(string? baseUrl, string? name, string? model)
    {
        var u = baseUrl?.Trim().ToLowerInvariant() ?? "";
        var n = $"{name} {model}".ToLowerInvariant();

        if (u.Contains("deepseek") || n.Contains("deepseek"))
            return "DeepSeek";
        if (u.Contains("dashscope") || u.Contains("aliyun") || n.Contains("qwen") || n.Contains("百炼") || n.Contains("bailian"))
            return "阿里云百炼";
        if (u.Contains("openai.com") || n.Contains("gpt-"))
            return "OpenAI";

        return "OpenAI 兼容（自建）";
    }

    private static bool IsGenericName(string name) =>
        name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
        || name.Contains("兼容", StringComparison.OrdinalIgnoreCase)
        || name.Equals("openai", StringComparison.OrdinalIgnoreCase);

    private static string? TryGetHost(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
            return null;
        return uri.Host;
    }
}
