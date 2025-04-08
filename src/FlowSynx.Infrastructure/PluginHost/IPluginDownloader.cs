namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginDownloader
{
    Task<byte[]> GetPluginDataAsync(string url);
    Task<PluginInstallMetadata> GetPluginMetadataAsync(string url);
    Task<string> ExtractPluginAsync(string pluginPath, CancellationToken cancellationToken);
    bool ValidateChecksum(string pluginPath, string expectedChecksum);
}