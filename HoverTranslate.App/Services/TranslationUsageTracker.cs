using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

/// <summary>最近一次翻译用量（本会话累计），供设置页与译文窗展示。</summary>
public static class TranslationUsageTracker
{
    public sealed record Snapshot(
        string Provider,
        string? Model,
        int? PromptTokens,
        int? CompletionTokens,
        int? TotalTokens,
        DateTime AtUtc);

    public static Snapshot? Last { get; private set; }
    public static long SessionTotalTokens { get; private set; }
    public static int SessionRequestCount { get; private set; }

    public static void Record(TranslationResult result)
    {
        if (!result.Success)
            return;

        Last = new Snapshot(
            result.Provider,
            result.Model,
            result.PromptTokens,
            result.CompletionTokens,
            result.TotalTokens,
            DateTime.UtcNow);

        SessionRequestCount++;
        if (result.TotalTokens is > 0)
            SessionTotalTokens += result.TotalTokens.Value;
        else if (result.PromptTokens is > 0 || result.CompletionTokens is > 0)
            SessionTotalTokens += (result.PromptTokens ?? 0) + (result.CompletionTokens ?? 0);
    }

    public static string FormatForSettings(AppConfig config)
    {
        var channel = ApiProfileDisplay.FormatActiveChannel(config);
        if (Last is null)
            return $"当前通道：{channel}。完成一次翻译后将显示模型与 token 用量。";

        var usage = Last.TotalTokens is > 0
            ? $"上次 {Last.TotalTokens} tokens"
            : Last.PromptTokens is > 0 || Last.CompletionTokens is > 0
                ? $"上次 {(Last.PromptTokens ?? 0) + (Last.CompletionTokens ?? 0)} tokens"
                : "上次未返回 token 统计";

        var model = !string.IsNullOrWhiteSpace(Last.Model) ? Last.Model : Last.Provider;
        return $"当前通道：{channel} · 最近使用 {model} · {usage} · 本会话累计 {SessionTotalTokens} tokens（{SessionRequestCount} 次）";
    }
}
