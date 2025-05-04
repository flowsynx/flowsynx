using FlowSynx.Infrastructure.Extensions;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public class PluginCacheIndex(string userId, string pluginType, string pluginVersion)
{
    public string UserId { get; } = userId;
    public string PluginType { get; } = pluginType;
    public string PluginVersion { get; } = pluginVersion;

    public override string ToString()
    {
        var key = $"{UserId}:{PluginType}:{PluginVersion}";
        return key.Md5HashKey();
    }
}