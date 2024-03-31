using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

internal class AzureBlobStorageSpecifications
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; }
}