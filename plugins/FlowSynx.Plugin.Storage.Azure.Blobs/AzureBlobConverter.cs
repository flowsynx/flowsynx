using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FlowSynx.IO;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

static class AzureBlobConverter
{
    public static StorageEntity ToEntity(BlobContainerClient client)
    {
        var entity = new StorageEntity(client.Name, StorageEntityItemKind.Directory)
        {
            Metadata =
            {
                ["IsContainer"] = true
            }
        };
        if (client.Name == "$logs")
        {
            entity.Metadata["IsLogsContainer"] = true;
        }
        return entity;
    }

    public static StorageEntity ToEntity(string containerName, BlobHierarchyItem item)
    {
        var fullPath = PathHelper.Combine(containerName, item.Blob.Name);
        var entity = new StorageEntity(fullPath, StorageEntityItemKind.File)
        {
            Md5 = item.Blob.Properties.ContentHash.ToHexString(),
            Size = item.Blob.Properties.ContentLength,
            ModifiedTime = item.Blob.Properties.LastModified
        };

        AddProperties(entity, item.Blob.Properties);

        return entity;
    }

    public static StorageEntity ToEntity(string containerName, string prefix)
    {
        var fullPath = PathHelper.Combine(containerName, prefix);
        var entity = new StorageEntity(fullPath, StorageEntityItemKind.Directory);
        return entity;
    }

    private static void AddProperties(StorageEntity entity, BlobItemProperties properties)
    {
        entity.TryAddMetadata(
            "CustomerProvidedKeySha256", properties.CustomerProvidedKeySha256,
            "IncrementalCopy", properties.IncrementalCopy,
            "ServerEncrypted", properties.ServerEncrypted,
            "DeletedOn", properties.DeletedOn,
            "RemainingRetentionDays", properties.RemainingRetentionDays,
            "AccessTier", properties.AccessTier,
            "AccessTierChangedOn", properties.AccessTierChangedOn,
            "AccessTierInferred", properties.AccessTierInferred,
            "ArchiveStatus", properties.ArchiveStatus,
            "BlobSequenceNumber", properties.BlobSequenceNumber,
            "BlobType", properties.BlobType,
            "CacheControl", properties.CacheControl,
            "ContentDisposition", properties.ContentDisposition,
            "ContentEncoding", properties.ContentEncoding,
            "ContentHash", properties.ContentHash.ToHexString(),
            "ContentLanguage", properties.ContentLanguage,
            "ContentLength", properties.ContentLength,
            "ContentType", properties.ContentType,
            "CopyCompletedOn", properties.CopyCompletedOn,
            "CopyId", properties.CopyId,
            "CopyProgress", properties.CopyProgress,
            "CopySource", properties.CopySource,
            "CopyStatus", properties.CopyStatus,
            "CopyStatusDescription", properties.CopyStatusDescription,
            "CreatedOn", properties.CreatedOn,
            "DestinationSnapshot", properties.DestinationSnapshot,
            "ETag", properties.ETag,
            "LastModified", properties.LastModified,
            "LeaseDuration", properties.LeaseDuration,
            "LeaseState", properties.LeaseState,
            "LeaseStatus", properties.LeaseStatus);
    }
    
    public static string ToHexString(this byte[]? bytes)
    {
        return bytes == null ? string.Empty : System.Convert.ToHexString(bytes);
    }
}