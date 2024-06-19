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
        if (properties.AccessTier.HasValue)
            entity.Metadata.Add("AccessTier", properties.AccessTier);

        if (properties.AccessTierChangedOn.HasValue)
            entity.Metadata.Add("AccessTierChangedOn", properties.AccessTierChangedOn);

        entity.Metadata.Add("AccessTierInferred", properties.AccessTierInferred);

        if (properties.BlobSequenceNumber.HasValue)
            entity.Metadata.Add("BlobSequenceNumber", properties.BlobSequenceNumber);

        if (properties.BlobType.HasValue)
            entity.Metadata.Add("BlobType", properties.BlobType);

        entity.Metadata.Add("CacheControl", properties.CacheControl);
        entity.Metadata.Add("ContentDisposition", properties.ContentDisposition);
        entity.Metadata.Add("ContentEncoding", properties.ContentEncoding);
        entity.Metadata.Add("ContentHash", properties.ContentHash.ToHexString());
        entity.Metadata.Add("ContentLanguage", properties.ContentLanguage);
        entity.Metadata.Add("CustomerProvidedKeySha256", properties.CustomerProvidedKeySha256);
        
        if (properties.ContentLength.HasValue)
            entity.Metadata.Add("ContentLength", properties.ContentLength);

        entity.Metadata.Add("ContentType", properties.ContentType);

        if (properties.CopyCompletedOn.HasValue)
            entity.Metadata.Add("CopyCompletedOn", properties.CopyCompletedOn);

        entity.Metadata.Add("CopyId", properties.CopyId);
        entity.Metadata.Add("CopyProgress", properties.CopyProgress);
        entity.Metadata.Add("CopySource", properties.CopySource);

        if (properties.CopyStatus.HasValue)
            entity.Metadata.Add("CopyStatus", properties.CopyStatus);

        entity.Metadata.Add("CopyStatusDescription", properties.CopyStatusDescription);

        if (properties.CreatedOn.HasValue)
            entity.Metadata.Add("CreatedOn", properties.CreatedOn);

        if (properties.DeletedOn.HasValue)
            entity.Metadata.Add("DeletedOn", properties.DeletedOn);

        entity.Metadata.Add("DestinationSnapshot", properties.DestinationSnapshot);

        if (properties.ETag.HasValue)
            entity.Metadata.Add("ETag", properties.ETag);

        if (properties.IncrementalCopy.HasValue)
            entity.Metadata.Add("IncrementalCopy", properties.IncrementalCopy);

        if (properties.LastModified.HasValue)
            entity.Metadata.Add("LastModified", properties.LastModified);

        if (properties.LeaseDuration.HasValue)
            entity.Metadata.Add("LeaseDuration", properties.LeaseDuration);

        if (properties.LeaseState.HasValue)
            entity.Metadata.Add("LeaseState", properties.LeaseState);

        if (properties.LeaseStatus.HasValue)
            entity.Metadata.Add("LeaseStatus", properties.LeaseStatus);
        
        if (properties.RemainingRetentionDays.HasValue)
            entity.Metadata.Add("RemainingRetentionDays", properties.RemainingRetentionDays);

        if (properties.ServerEncrypted.HasValue)
            entity.Metadata.Add("ServerEncrypted", properties.ServerEncrypted);
    }
}