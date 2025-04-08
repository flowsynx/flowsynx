using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginCacheService
{
    IPlugin? Get(string key);
    void Set(string key, IPlugin value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    bool TryGetValue(string key, out IPlugin? value);
    void Remove(string key);
}