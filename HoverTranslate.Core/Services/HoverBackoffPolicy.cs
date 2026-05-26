namespace HoverTranslate.Core.Services;

/// <summary>429/限速后的冷却与重试退避。</summary>
public sealed class HoverBackoffPolicy
{
    private readonly object _lock = new();
    private DateTime _pausedUntil = DateTime.MinValue;
    private int _consecutiveRateLimits;

    public bool IsPaused
    {
        get
        {
            lock (_lock)
                return DateTime.UtcNow < _pausedUntil;
        }
    }

    public void RecordRateLimit(int baseMs, int maxMs)
    {
        lock (_lock)
        {
            _consecutiveRateLimits++;
            var exponent = Math.Min(_consecutiveRateLimits - 1, 6);
            var delay = Math.Min(maxMs, baseMs * (1 << exponent));
            var until = DateTime.UtcNow.AddMilliseconds(delay);
            if (until > _pausedUntil)
                _pausedUntil = until;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
            _consecutiveRateLimits = 0;
    }

    public int GetRetryDelayMs(int attemptIndex, int baseMs) =>
        Math.Min(30_000, baseMs * (1 << Math.Min(attemptIndex, 4)));
}
