using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

public static class RateLimitDetector
{
    public static bool IsRateLimited(TranslationResult result)
    {
        if (result.Success) return false;
        var message = result.ErrorMessage ?? "";
        if (message.Length == 0) return false;

        if (message.Contains("429", StringComparison.Ordinal)
            || message.Contains("503", StringComparison.Ordinal))
            return true;

        return message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
               || message.Contains("too many", StringComparison.OrdinalIgnoreCase)
               || message.Contains("quota", StringComparison.OrdinalIgnoreCase)
               || message.Contains("throttl", StringComparison.OrdinalIgnoreCase)
               || message.Contains("限速", StringComparison.OrdinalIgnoreCase)
               || message.Contains("频繁", StringComparison.OrdinalIgnoreCase)
               || message.Contains("请求过多", StringComparison.OrdinalIgnoreCase);
    }
}
