namespace HoverTranslate.Core.Services;

/// <summary>滑动窗口限流：每秒最多 N 次请求，超出则丢弃。</summary>
public sealed class SlidingWindowRateLimiter
{
    private readonly object _lock = new();
    private readonly Queue<DateTime> _timestamps = new();

    public bool TryAcquire(double maxPerSecond)
    {
        if (maxPerSecond <= 0) return false;

        var limit = (int)Math.Ceiling(maxPerSecond);
        var window = TimeSpan.FromSeconds(1);
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            while (_timestamps.Count > 0 && now - _timestamps.Peek() > window)
                _timestamps.Dequeue();

            if (_timestamps.Count >= limit)
                return false;

            _timestamps.Enqueue(now);
            return true;
        }
    }
}
