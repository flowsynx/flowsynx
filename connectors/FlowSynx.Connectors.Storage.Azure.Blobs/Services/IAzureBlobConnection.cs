using Azure.Storage.Blobs;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public interface IAzureBlobConnection
{
    BlobServiceClient GetClient(AzureBlobSpecifications specifications);
}