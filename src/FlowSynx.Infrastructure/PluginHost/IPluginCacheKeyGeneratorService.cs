namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginCacheKeyGeneratorService
{
    string GenerateKey(string userId, string pluginType, string pluginVersion, object? specifications);
}