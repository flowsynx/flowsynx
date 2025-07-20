namespace FlowSynx.Application.Configuration;

public class ResultStorageConfiguration
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<ResultStorageProviderConfiguration> Providers { get; set; } = new();
}

public class ResultStorageProviderConfiguration
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!; // "Local", "AzureBlob", "S3"
    public Dictionary<string, string> Configuration { get; set; } = new();
}