using Azure.Storage.Blobs;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public interface IAzureBlobClientHandler
{
    BlobServiceClient GetClient(AzureBlobSpecifications specifications);
}