using Azure.Storage;
using Azure.Storage.Blobs;
using FlowSynx.Plugins.Azure.Blobs.Models;

namespace FlowSynx.Plugins.Azure.Blobs.Services;

public class AzureBlobConnection: IAzureBlobConnection
{
   public BlobServiceClient Connect(AzureBlobSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new Exception(Resources.PropertiesShouldHaveValue);

        var uriString = $"https://{specifications.AccountName}.blob.core.windows.net";
        var uri = new Uri(uriString);
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new BlobServiceClient(serviceUri: uri, credential: credential);
    }
}