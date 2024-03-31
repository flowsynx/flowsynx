using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFilesSpecifications
{
    [RequiredMember]
    public string? ShareName { get; set; }

    public string? AccountKey { get; set; }

    public string? AccountName { get; set; }

    public string? ConnectionString { get; set; }
}