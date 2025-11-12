using System.Collections.Concurrent;
using FlowSynx.Infrastructure.PluginHost.PluginLoaders;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public class PluginCacheService : IPluginCacheService
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyIndex = new();
    private readonly Lock _indexLock = new();

    public IPluginLoader? Get(string key)
    {
        return _cache.TryGetValue(key, out IPluginLoader? value) ? value : null;
    }

    public bool TryGetValue(string key, out IPluginLoader? value)
    {
        if (_cache.TryGetValue(key, out var raw))
        {
            value = (IPluginLoader?)raw;
            return true;
        }

        value = null;
        return false;
    }

    public void Set(string key, PluginCacheIndex index, IPluginLoader value,
        TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpiration.HasValue)
            options.SetAbsoluteExpiration(absoluteExpiration.Value);
        if (slidingExpiration.HasValue)
            options.SetSlidingExpiration(slidingExpiration.Value);

        RegisterEvictionCallback(options);

        _cache.Set(key, value, options);

        var indexKey = index.ToString();

        lock (_indexLock)
        {
            if (!_keyIndex.ContainsKey(indexKey))
                _keyIndex[indexKey] = new HashSet<string>();

            _keyIndex[indexKey].Add(key);
        }
    }

    public void RemoveByKey(string key)
    {
        _cache.Remove(key);

        lock (_indexLock)
        {
            foreach (var entry in _keyIndex)
            {
                if (entry.Value.Remove(key) && entry.Value.Count == 0)
                    _keyIndex.TryRemove(entry.Key, out _);
            }
        }
    }

    public void RemoveByIndex(PluginCacheIndex index)
    {
        var indexKey = index.ToString();

        lock (_indexLock)
        {
            if (!_keyIndex.TryGetValue(indexKey, out var keys)) return;

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }

            _keyIndex.TryRemove(indexKey, out _);
        }
    }

    /// <summary>
    /// Attaches a post-eviction callback so plugin handles are unloaded once the cache expires or is cleared.
    /// </summary>
    private void RegisterEvictionCallback(MemoryCacheEntryOptions options)
    {
        options.RegisterPostEvictionCallback((_, evictedValue, reason, _) =>
        {
            if ((reason is EvictionReason.Expired or EvictionReason.Removed) &&
                evictedValue is IPluginLoader pluginHandle)
            {
                pluginHandle.Unload();
            }
        });
    }
}
