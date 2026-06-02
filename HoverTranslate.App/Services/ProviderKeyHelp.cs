using System.Diagnostics;
using HoverTranslate.Core.Models;

namespace HoverTranslate.App.Services;

internal static class ProviderKeyHelp
{
    internal sealed record KeyLink(string Label, string Url, string? Hint = null);

    internal static KeyLink? TryDetectOpenAiLink(string? baseUrl, string? profileName)
    {
        var u = baseUrl?.Trim().ToLowerInvariant() ?? "";
        var n = profileName?.Trim().ToLowerInvariant() ?? "";

        if (u.Contains("deepseek") || n.Contains("deepseek"))
            return new KeyLink("DeepSeek", "https://platform.deepseek.com/api_keys",
                "登录后在 API Keys 页面创建密钥，Base URL 保持 https://api.deepseek.com。");

        if (u.Contains("dashscope") || u.Contains("aliyun") || n.Contains("百炼") || n.Contains("bailian"))
            return new KeyLink("阿里云百炼", "https://dashscope.console.aliyun.com/apiKey",
                "在百炼控制台创建 API Key；Base URL 一般为 https://dashscope.aliyuncs.com/compatible-mode/v1。");

        if (u.Contains("openai.com") || n.Contains("openai"))
            return new KeyLink("OpenAI", "https://platform.openai.com/api-keys",
                "在 OpenAI 平台创建 API Key，Base URL 为 https://api.openai.com/v1。");

        return null;
    }

    internal static IReadOnlyList<KeyLink> GetOpenAiLinks() =>
    [
        new("DeepSeek", "https://platform.deepseek.com/api_keys",
            "兼容 OpenAI 接口；模型如 deepseek-chat。"),
        new("阿里云百炼", "https://dashscope.console.aliyun.com/apiKey",
            "国内节点；需填写 DashScope 兼容 Base URL 与模型名。")
    ];

    internal static KeyLink GetPrimaryLink(string kind, string? baseUrl, string? profileName) =>
        kind switch
        {
            ApiProfileKinds.Microsoft => new KeyLink("Azure 翻译", "https://portal.azure.com/#create/Microsoft.CognitiveServicesTextTranslation",
                "创建 Translator 资源后，在「密钥与终结点」复制 Key 与区域。"),
            ApiProfileKinds.Google => new KeyLink("Google Cloud", "https://console.cloud.google.com/apis/library/translate.googleapis.com",
                "启用 Cloud Translation API 后，在「凭据」创建 API Key。"),
            _ => TryDetectOpenAiLink(baseUrl, profileName)
                 ?? GetOpenAiLinks()[0]
        };

    internal static string GetHelpButtonLabel(string kind, string? baseUrl, string? profileName)
    {
        if (kind == ApiProfileKinds.Microsoft) return "Azure 获取密钥 ↗";
        if (kind == ApiProfileKinds.Google) return "Google 获取密钥 ↗";
        var detected = TryDetectOpenAiLink(baseUrl, profileName);
        return detected is null ? "获取 API Key ▾" : $"{detected.Label} 获取密钥 ↗";
    }

    internal static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
