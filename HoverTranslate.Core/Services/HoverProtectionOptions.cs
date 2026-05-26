using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

public sealed class HoverProtectionOptions
{
    public int MinDebounceMs { get; init; } = 400;
    public int CacheTtlSeconds { get; init; } = 300;
    public int CacheMaxEntries { get; init; } = 256;
    public double MaxRequestsPerSecond { get; init; } = 2.0;
    public int MaxRetries { get; init; } = 2;
    public int BackoffBaseMs { get; init; } = 2000;
    public int BackoffMaxMs { get; init; } = 60_000;

    public static HoverProtectionOptions FromConfig(AppConfig config) => new()
    {
        MinDebounceMs = Math.Clamp(config.HoverMinDebounceMs, 300, 5000),
        CacheTtlSeconds = Math.Clamp(config.HoverCacheTtlSeconds, 30, 3600),
        CacheMaxEntries = Math.Clamp(config.HoverCacheMaxEntries, 32, 2000),
        MaxRequestsPerSecond = Math.Clamp(config.HoverMaxRequestsPerSecond, 0.5, 10),
        MaxRetries = Math.Clamp(config.HoverRetryMaxAttempts, 1, 5),
        BackoffBaseMs = Math.Clamp(config.HoverBackoffBaseMs, 500, 30_000),
        BackoffMaxMs = Math.Clamp(config.HoverBackoffMaxMs, 2000, 300_000)
    };

    public int EffectiveDebounceMs(AppConfig config) =>
        Math.Max(config.HoverDelayMs, MinDebounceMs);
}
