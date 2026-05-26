using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

public enum HoverSkipReason
{
    None,
    Backoff,
    RateLimit
}

public sealed class HoverProtectionResult
{
    public bool AttemptedApi { get; init; }
    public bool FromCache { get; init; }
    public HoverSkipReason SkipReason { get; init; }
    public TranslationResult? Result { get; init; }

    public static HoverProtectionResult Cached(TranslationResult result) => new()
    {
        AttemptedApi = false,
        FromCache = true,
        Result = result
    };

    public static HoverProtectionResult Ok(TranslationResult result) => new()
    {
        AttemptedApi = true,
        Result = result
    };

    public static HoverProtectionResult Failed(TranslationResult result) => new()
    {
        AttemptedApi = true,
        Result = result
    };

    public static HoverProtectionResult Skipped(HoverSkipReason reason) => new()
    {
        SkipReason = reason
    };
}
