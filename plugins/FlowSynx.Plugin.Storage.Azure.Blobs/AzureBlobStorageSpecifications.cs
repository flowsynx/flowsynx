using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

internal class AzureBlobStorageSpecifications
{
    [RequiredMember]
    public string? AccountName { get; set; }

    [RequiredMember]
    public string? AccountKey { get; set; }

}