using Azure.Storage.Blobs;
using FlowSynx.Plugins.Azure.Blobs.Models;

namespace FlowSynx.Plugins.Azure.Blobs.Services;

public interface IAzureBlobConnection
{
    BlobServiceClient Connect(AzureBlobSpecifications specifications);
}