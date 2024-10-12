using FlowSynx.Abstractions.Attributes;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Storage.Azure.Blobs;

internal class AzureBlobStorageSpecifications: Specifications
{
    [RequiredMember]
    public string AccountName { get; set; } = string.Empty;

    [RequiredMember]
    public string AccountKey { get; set; } = string.Empty;

}