using FlowSynx.Infrastructure.Extensions;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public record class PluginCacheIndex(string UserId, string PluginType, string PluginVersion)
{
    public override string ToString()
    {
        var key = $"{UserId}:{PluginType}:{PluginVersion}";
        return key.Md5HashKey();
    }
}