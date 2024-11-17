using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.CacheSample;

[Capsule]
public class MemoryCache(TimeSpan staleAfter)
{
    private readonly Dictionary<int, Entry> _cacheEntries = new();

    [Expose]
    public async Task InsertOrUpdateAsync(int key, string content)
    {
        _cacheEntries[key] = new Entry(DateTime.UtcNow, content);
    }

    [Expose]
    public async Task<string?> GetAsync(int key)
    {
        return _cacheEntries.GetValueOrDefault(key)?.Content;
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
    public async Task RemoveStaleEntriesAsync()
    {
        var staleEntryKeys = _cacheEntries
            .Where(p => p.Value.Timestamp < DateTime.UtcNow - staleAfter)
            .Select(p => p.Key)
            .ToList();

        foreach (var key in staleEntryKeys)
        {
            _cacheEntries.Remove(key);
        }
    }

    private record Entry(DateTime Timestamp, string Content);
}
