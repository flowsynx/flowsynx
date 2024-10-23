using Azure.Storage;
using Azure.Storage.Files.Shares;
using FlowSynx.Connectors.Storage.Azure.Files.Models;
using FlowSynx.Connectors.Storage.Exceptions;

namespace FlowSynx.Connectors.Storage.Azure.Files.Services;

public class AzureFilesConnection: IAzureFilesConnection
{
    public ShareClient GetClient(AzureFilesSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.ShareName))
            throw new StorageException(Resources.ShareNameInSpecificationShouldBeNotEmpty);

        if (!string.IsNullOrEmpty(specifications.ConnectionString))
            return new ShareClient(specifications.ConnectionString, specifications.ShareName);

        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new StorageException(Resources.OnePropertyShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.file.core.windows.net/{specifications.ShareName}");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new ShareClient(shareUri: uri, credential: credential);
    }
}