using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

/// <summary>悬停译文缓存：同一原文在 TTL 内不重复请求 API。</summary>
public sealed class TranslationResultCache
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxEntries;

    public TranslationResultCache(int maxEntries) => _maxEntries = Math.Max(16, maxEntries);

    public static string NormalizeKey(string text) => text.Trim();

    public bool TryGet(string key, out TranslationResult result)
    {
        lock (_lock)
        {
            PruneExpired();
            if (!_entries.TryGetValue(key, out var entry))
            {
                result = null!;
                return false;
            }

            if (entry.ExpiresAt <= DateTime.UtcNow)
            {
                _entries.Remove(key);
                result = null!;
                return false;
            }

            result = entry.Result;
            return true;
        }
    }

    public void Set(string key, TranslationResult result, TimeSpan ttl)
    {
        lock (_lock)
        {
            PruneExpired();
            _entries[key] = new Entry(result, DateTime.UtcNow.Add(ttl));
            while (_entries.Count > _maxEntries)
                EvictOldest();
        }
    }

    private void PruneExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var pair in _entries.Where(p => p.Value.ExpiresAt <= now).ToList())
            _entries.Remove(pair.Key);
    }

    private void EvictOldest()
    {
        var oldest = _entries.OrderBy(p => p.Value.ExpiresAt).FirstOrDefault();
        if (oldest.Key is not null)
            _entries.Remove(oldest.Key);
    }

    private sealed record Entry(TranslationResult Result, DateTime ExpiresAt);
}
