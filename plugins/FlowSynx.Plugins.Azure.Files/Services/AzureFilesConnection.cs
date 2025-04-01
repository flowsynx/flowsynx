using Azure.Storage;
using Azure.Storage.Files.Shares;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Plugins.Azure.Files.Exceptions;
using FlowSynx.Plugins.Azure.Files.Models;

namespace FlowSynx.Plugins.Azure.Files.Services;

public class AzureFilesConnection: IAzureFilesConnection
{
    public ShareClient Connect(AzureFilesSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.ShareName))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareNameMustNotEmpty, Resources.ShareNameInSpecificationShouldBeNotEmpty);

        if (!string.IsNullOrEmpty(specifications.ConnectionString))
            return new ShareClient(specifications.ConnectionString, specifications.ShareName);

        if (string.IsNullOrEmpty(specifications.AccountKey))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesAccountKeyMustNotEmpty, Resources.OnePropertyShouldHaveValue);

        if (string.IsNullOrEmpty(specifications.AccountName))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesAccountNameMustNotEmpty, Resources.OnePropertyShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.file.core.windows.net/{specifications.ShareName}");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new ShareClient(shareUri: uri, credential: credential);
    }
}