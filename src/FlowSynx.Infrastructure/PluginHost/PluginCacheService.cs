using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Infrastructure.PluginHost;

using FlowSynx.PluginCore;
using System.Collections.Concurrent;

public class PluginCacheService : IPluginCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyIndex = new();
    private readonly object _indexLock = new();

    public PluginCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public PluginLoader? Get(string key)
    {
        return _cache.TryGetValue(key, out PluginLoader? value) ? value : default;
    }

    public bool TryGetValue(string key, out PluginLoader? value)
    {
        if (_cache.TryGetValue(key, out var raw))
        {
            value = (PluginLoader?)raw;
            return true;
        }

        value = default;
        return false;
    }

    public void Set(string key, PluginCacheIndex index, IPlugin value,
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

        if (_keyIndex.TryGetValue(indexKey, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }

            _keyIndex.TryRemove(indexKey, out _);
        }
    }

    private void RegisterEvictionCallback(MemoryCacheEntryOptions options)
    {
        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            if (reason == EvictionReason.Expired || reason == EvictionReason.Removed)
            {
                if (evictedValue is IPlugin pluginHandle)
                {
                    pluginHandle = null;
                }
            }
        });
    }
}