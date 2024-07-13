using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace FlowSynx.Plugin.Storage.Azure.Files;

static class AzureFileConverter
{
    public static async Task<StorageEntity> ToEntity(this ShareDirectoryClient shareDirectoryClient, 
        ShareFileItem fileItem, ShareFileClient client, bool? includeMetadata, 
        CancellationToken cancellationToken)
    {
        var fileProperties = await client.GetPropertiesAsync(cancellationToken);
        var entity = new StorageEntity(shareDirectoryClient.Path, fileItem.Name, StorageEntityItemKind.File)
        {
            ModifiedTime = fileProperties.Value.LastModified,
            Size = fileItem.FileSize,
            Md5 = fileProperties.Value.ContentHash!=null ? System.Text.Encoding.UTF8.GetString(fileProperties.Value.ContentHash) : null,
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata(
                "CopyStatus", fileProperties.Value.CopyStatus.ToString(),
                "ChangeTime", fileItem.Properties.ChangedOn?.ToString() ?? string.Empty,
                "CreationTime", fileItem.Properties.CreatedOn?.ToString() ?? string.Empty,
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