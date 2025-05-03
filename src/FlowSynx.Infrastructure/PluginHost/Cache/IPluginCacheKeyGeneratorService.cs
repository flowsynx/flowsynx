namespace FlowSynx.Infrastructure.PluginHost.Cache;

public interface IPluginCacheKeyGeneratorService
{
    string GenerateKey(string userId, string pluginType, string pluginVersion, object? specifications);
}