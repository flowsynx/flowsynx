using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFilesSpecifications
{
    [RequiredMember]
    public string ShareName { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string AccountKey { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;
}