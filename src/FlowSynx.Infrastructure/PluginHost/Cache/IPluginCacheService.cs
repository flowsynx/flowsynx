using FlowSynx.Infrastructure.PluginHost.PluginLoaders;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public interface IPluginCacheService
{
    IPluginLoader? Get(string key);
    void Set(string key, PluginCacheIndex index, IPluginLoader value,
        TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    bool TryGetValue(string key, out IPluginLoader? value);
    void RemoveByKey(string key);
    void RemoveByIndex(PluginCacheIndex index);
}