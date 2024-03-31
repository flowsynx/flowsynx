using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace FlowSynx.Plugin.Storage.Azure.Files;

static class AzureFileConverter
{
    public static async Task<StorageEntity> ToEntity(string path, ShareFileItem fileItem, ShareFileClient client, CancellationToken cancellationToken)
    {
        var fileProperties = await client.GetPropertiesAsync(cancellationToken);
        var entity = new StorageEntity(path, fileItem.Name, StorageEntityItemKind.File)
        {
            ModifiedTime = fileProperties.Value.LastModified,
            Size = fileItem.FileSize,
            Md5 = fileProperties.Value.ContentHash!=null ? System.Text.Encoding.UTF8.GetString(fileProperties.Value.ContentHash) : null,
        };

        entity.TryAddMetadata(
            "CopyStatus", fileProperties.Value.CopyStatus.ToString(),
            "ChangeTime", fileItem.Properties.ChangedOn?.ToString() ?? string.Empty,
            "CreationTime", fileItem.Properties.CreatedOn?.ToString() ?? string.Empty,
            "ETag", fileProperties.Value.ETag,
            "IsServerEncrypted", fileProperties.Value.IsServerEncrypted.ToString(),
            "NtfsAttributes", fileItem.FileAttributes.ToString() ?? string.Empty);

        return entity;
    }

    public static async Task<StorageEntity> ToEntity(string path, ShareFileItem fileItem, ShareDirectoryClient client, CancellationToken cancellationToken)
    {
        var fileProperties = await client.GetPropertiesAsync(cancellationToken);
        var entity = new StorageEntity(path, fileItem.Name, StorageEntityItemKind.Directory)
        {
            ModifiedTime = fileProperties.Value.LastModified
        };
        entity.TryAddMetadata(
            "ChangeTime", fileItem.Properties.ChangedOn?.ToString() ?? string.Empty,
            "CreationTime", fileItem.Properties.CreatedOn?.ToString() ?? string.Empty,
            "ETag", fileProperties.Value.ETag,
            "IsServerEncrypted", fileProperties.Value.IsServerEncrypted.ToString(),
            "NtfsAttributes", fileItem.FileAttributes.ToString() ?? string.Empty);

        return entity;
    }
}