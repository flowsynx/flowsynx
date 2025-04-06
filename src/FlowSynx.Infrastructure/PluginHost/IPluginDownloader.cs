namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginDownloader
{
    Task<byte[]> GetPluginDataAsync(string url);
    Task<PluginInstallMetadata> GetPluginMetadataAsync(string url);
}