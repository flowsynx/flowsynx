using FlowSynx.Abstractions.Attributes;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Storage.Azure.Files;

public class AzureFilesSpecifications: Specifications
{
    [RequiredMember]
    public string ShareName { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string AccountKey { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;
}