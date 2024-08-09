using FlowSynx.Abstractions.Attributes;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

internal class AzureBlobStorageSpecifications
{
    [RequiredMember]
    public string AccountName { get; set; } = string.Empty;

    [RequiredMember]
    public string AccountKey { get; set; } = string.Empty;

}