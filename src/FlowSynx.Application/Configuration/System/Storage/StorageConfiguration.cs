namespace FlowSynx.Application.Configuration.System.Storage;

public class StorageConfiguration
{
    public long MaxSizeLimit { get; set; } = 200 * 1024 * 1024; // Default 200 MB
    public ResultStorageConfiguration ResultStorage { get; set; } = new();
}