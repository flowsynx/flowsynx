namespace FlowSynx.Infrastructure.PluginHost.Manager;

public interface IPluginDownloader
{
    Task<byte[]> GetPluginDataAsync(string url, string pluginType, 
        string pluginVersion, CancellationToken cancellationToken);

    Task<PluginInstallMetadata> GetPluginMetadataAsync(string url, string pluginType, 
        string pluginVersion, CancellationToken cancellationToken);

    Task<IEnumerable<PluginVersion>> GetPluginVersionsAsync(string url, string pluginType, 
        CancellationToken cancellationToken);

    Task ExtractPluginAsync(string pluginDirectory, byte[] data, 
        CancellationToken cancellationToken);

    bool ValidateChecksum(byte[] data, string? expectedChecksum);
}