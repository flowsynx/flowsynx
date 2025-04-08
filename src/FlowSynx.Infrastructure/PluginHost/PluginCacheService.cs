using FlowSynx.PluginCore;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginCacheService : IPluginCacheService
{
    private readonly IMemoryCache _cache;

    public PluginCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public IPlugin? Get(string key)
    {
        return _cache.TryGetValue(key, out IPlugin? value) ? value : default;
    }

    public bool TryGetValue(string key, out IPlugin? value)
    {
        if (_cache.TryGetValue(key, out var raw))
        {
            value = (IPlugin?)raw;
            return true;
        }

        value = default;
        return false;
    }

    public void Set(string key, IPlugin value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        var options = new MemoryCacheEntryOptions();

        if (absoluteExpiration.HasValue)
            options.SetAbsoluteExpiration(absoluteExpiration.Value);

        if (slidingExpiration.HasValue)
            options.SetSlidingExpiration(slidingExpiration.Value);

        _cache.Set(key, value, options);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}