using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.IO;

namespace FlowSynx.Connectors.Storage.Azure.Files;

static class AzureFileConverter
{
    public static async Task<StorageEntity> ToEntity(this ShareDirectoryClient shareDirectoryClient, 
        ShareFileItem fileItem, ShareFileClient client, bool? includeMetadata, 
        CancellationToken cancellationToken)
    {
        var fileProperties = await client.GetPropertiesAsync(cancellationToken);
        var entity = new StorageEntity(shareDirectoryClient.Path, fileItem.Name, StorageEntityItemKind.File)
        {
            CreatedTime = fileItem.Properties.CreatedOn,
            ModifiedTime = fileProperties.Value.LastModified,
            Size = fileItem.FileSize
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata(
                "CopyStatus", fileProperties.Value.CopyStatus.ToString(),
                "ChangeTime", fileItem.Properties.ChangedOn?.ToString() ?? string.Empty,
                "CreationTime", fileItem.Properties.CreatedOn?.ToString() ?? string.Empty,
                "ContentHash", fileProperties.Value.ContentHash.ToHexString(),
                "ETag", fileProperties.Value.ETag,
                "IsServerEncrypted", fileProperties.Value.IsServerEncrypted.ToString(),
                "NtfsAttributes", fileItem.FileAttributes.ToString() ?? string.Empty);
        }

        return entity;
    }

    public static async Task<StorageEntity> ToEntity(this ShareDirectoryClient shareDirectoryClient, 
        ShareFileItem fileItem, bool? includeMetadata, CancellationToken cancellationToken)
    {
        var fileProperties = await shareDirectoryClient.GetPropertiesAsync(cancellationToken);
        var entity = new StorageEntity(shareDirectoryClient.Path, fileItem.Name, StorageEntityItemKind.Directory)
        {
            Size = 0,
            CreatedTime = fileItem.Properties.CreatedOn,
            ModifiedTime = fileProperties.Value.LastModified
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata(
                "ChangeTime", fileItem.Properties.ChangedOn?.ToString() ?? string.Empty,
                "CreationTime", fileItem.Properties.CreatedOn?.ToString() ?? string.Empty,
                "ETag", fileProperties.Value.ETag,
                "IsServerEncrypted", fileProperties.Value.IsServerEncrypted.ToString(),
                "NtfsAttributes", fileItem.FileAttributes.ToString() ?? string.Empty);
        }

        return entity;
    }
}