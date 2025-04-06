namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginExtractor
{
    Task<string> ExtractPluginAsync(string pluginPath, CancellationToken cancellationToken);
}