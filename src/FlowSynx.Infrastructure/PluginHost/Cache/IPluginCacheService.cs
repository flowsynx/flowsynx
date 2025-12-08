using FlowSynx.Infrastructure.PluginHost.Loader;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public interface IPluginCacheService
{
    PluginLoader? Get(string key);
    void Set(string key, PluginCacheIndex index, PluginLoader value,
        TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    bool TryGetValue(string key, out PluginLoader? value);
    void RemoveByKey(string key);
    void RemoveByIndex(PluginCacheIndex index);
}