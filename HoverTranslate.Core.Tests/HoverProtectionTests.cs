using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;
using Xunit;

namespace HoverTranslate.Core.Tests;

public class HoverProtectionTests
{
    [Fact]
    public void RateLimitDetector_Matches429AndKeywords()
    {
        Assert.True(RateLimitDetector.IsRateLimited(TranslationResult.Fail("x", "HTTP 429: Too Many Requests")));
        Assert.True(RateLimitDetector.IsRateLimited(TranslationResult.Fail("x", "请求过于频繁，请限速")));
        Assert.False(RateLimitDetector.IsRateLimited(TranslationResult.Fail("x", "invalid api key")));
    }

    [Fact]
    public void TranslationResultCache_ReturnsWithinTtl()
    {
        var cache = new TranslationResultCache(10);
        var key = TranslationResultCache.NormalizeKey("Hello");
        var result = TranslationResult.Ok("Hello", "你好", "test");

        cache.Set(key, result, TimeSpan.FromMinutes(5));
        Assert.True(cache.TryGet(key, out var hit));
        Assert.Equal("你好", hit.TranslatedText);
    }

    [Fact]
    public void SlidingWindowRateLimiter_BlocksOverLimit()
    {
        var limiter = new SlidingWindowRateLimiter();
        Assert.True(limiter.TryAcquire(2));
        Assert.True(limiter.TryAcquire(2));
        Assert.False(limiter.TryAcquire(2));
    }

    [Fact]
    public async Task HoverProtection_UsesCacheWithoutCallingApi()
    {
        var guard = new HoverProtectionService();
        var options = new HoverProtectionOptions { CacheTtlSeconds = 300, MaxRequestsPerSecond = 10 };
        var calls = 0;

        var first = await guard.TranslateAsync(
            "word",
            options,
            _ =>
            {
                calls++;
                return Task.FromResult(TranslationResult.Ok("word", "词", "t"));
            });

        var second = await guard.TranslateAsync(
            "word",
            options,
            _ =>
            {
                calls++;
                return Task.FromResult(TranslationResult.Ok("word", "词2", "t"));
            });

        Assert.True(first.AttemptedApi);
        Assert.True(second.FromCache);
        Assert.Equal(1, calls);
        Assert.Equal("词", second.Result!.TranslatedText);
    }

    [Fact]
    public void HoverProtectionOptions_EffectiveDebounce_UsesMaxOfDelayAndMin()
    {
        var config = new AppConfig { HoverDelayMs = 300, HoverMinDebounceMs = 400 };
        var options = HoverProtectionOptions.FromConfig(config);
        Assert.Equal(400, options.EffectiveDebounceMs(config));
    }
}
