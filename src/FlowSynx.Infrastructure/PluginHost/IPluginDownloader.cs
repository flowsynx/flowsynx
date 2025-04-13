namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginDownloader
{
    Task<byte[]> GetPluginDataAsync(string url);
    Task<PluginInstallMetadata> GetPluginMetadataAsync(string url, string pluginType, string pluginVersion);
    Task ExtractPluginAsync(string pluginDirectory, byte[] data, CancellationToken cancellationToken);
    bool ValidateChecksum(byte[] data, string expectedChecksum);
}