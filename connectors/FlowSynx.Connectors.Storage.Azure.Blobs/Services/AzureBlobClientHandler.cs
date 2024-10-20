using Azure.Storage;
using Azure.Storage.Blobs;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;
using FlowSynx.Connectors.Storage.Exceptions;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public class AzureBlobClientHandler: IAzureBlobClientHandler
{
   public BlobServiceClient GetClient(AzureBlobSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new StorageException(Resources.PropertiesShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.blob.core.windows.net");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new BlobServiceClient(serviceUri: uri, credential: credential);
    }
}