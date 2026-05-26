using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>悬停翻译防护：缓存、限流、退避重试。</summary>
public sealed class HoverProtectionService
{
    private TranslationResultCache? _cache;
    private int _cacheMaxEntries = 256;
    private readonly SlidingWindowRateLimiter _rateLimiter = new();
    private readonly HoverBackoffPolicy _backoff = new();

    public async Task<HoverProtectionResult> TranslateAsync(
        string text,
        HoverProtectionOptions options,
        Func<CancellationToken, Task<TranslationResult>> translate,
        CancellationToken cancellationToken = default)
    {
        EnsureCache(options.CacheMaxEntries);
        var key = TranslationResultCache.NormalizeKey(text);

        if (_cache!.TryGet(key, out var cached))
            return HoverProtectionResult.Cached(cached);

        if (_backoff.IsPaused)
            return HoverProtectionResult.Skipped(HoverSkipReason.Backoff);

        if (!_rateLimiter.TryAcquire(options.MaxRequestsPerSecond))
            return HoverProtectionResult.Skipped(HoverSkipReason.RateLimit);

        TranslationResult? last = null;
        var attempts = Math.Max(1, options.MaxRetries);

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (attempt > 0)
            {
                var delay = _backoff.GetRetryDelayMs(attempt - 1, options.BackoffBaseMs);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            last = await translate(cancellationToken).ConfigureAwait(false);

            if (last.Success)
            {
                _cache.Set(key, last, TimeSpan.FromSeconds(options.CacheTtlSeconds));
                _backoff.RecordSuccess();
                return HoverProtectionResult.Ok(last);
            }

            if (RateLimitDetector.IsRateLimited(last))
            {
                _backoff.RecordRateLimit(options.BackoffBaseMs, options.BackoffMaxMs);
                if (attempt < attempts - 1)
                    continue;
            }

            break;
        }

        return HoverProtectionResult.Failed(last!);
    }

    private void EnsureCache(int maxEntries)
    {
        if (_cache is not null && _cacheMaxEntries == maxEntries)
            return;

        _cacheMaxEntries = maxEntries;
        _cache = new TranslationResultCache(maxEntries);
    }
}
