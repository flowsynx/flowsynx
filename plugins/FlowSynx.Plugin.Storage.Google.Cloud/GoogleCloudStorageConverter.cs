﻿using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

static class GoogleCloudStorageConverter
{
    public static StorageEntity ToEntity(this string bucketName, bool? includeMetadata)
    {
        var entity = new StorageEntity(bucketName, StorageEntityItemKind.Directory);

        if (includeMetadata is true)
        {
            entity.Metadata["IsBucket"] = true;
        }

        return entity;
    }

    public static StorageEntity ToEntity(this Object googleObject, bool isDirectory, 
        bool? includeMetadata)
    {
        StorageEntity entity;
        var fullPath = PathHelper.Combine(googleObject.Bucket, googleObject.Name);

        if (isDirectory)
        {
            entity = new StorageEntity(fullPath, StorageEntityItemKind.Directory)
            {
                ModifiedTime = googleObject.UpdatedDateTimeOffset,
            };

            if (includeMetadata is true)
            {
                entity.Metadata["IsDirectory"] = true;
            }
        }
        else
        {
            entity = new StorageEntity(fullPath, StorageEntityItemKind.File)
            {
                ModifiedTime = googleObject.UpdatedDateTimeOffset,
                Md5 = Convert.FromBase64String(googleObject.Md5Hash).ToHexString(),
                Size = (long?)googleObject.Size
            };

            if (includeMetadata is true) {
                AddProperties(entity, googleObject);
            }
        }

        return entity;
    }
    
    private static void AddProperties(StorageEntity entity, Object googleObject)
    {
        entity.Metadata.Add("ContentType", googleObject.ContentType);
        entity.Metadata.Add("CacheControl", googleObject.CacheControl);

        if (googleObject.ComponentCount.HasValue)
            entity.Metadata.Add("ComponentControl", googleObject.ComponentCount);
        
        entity.Metadata.Add("ContentDisposition", googleObject.ContentDisposition);
        entity.Metadata.Add("ContentEncoding", googleObject.ContentEncoding);
        entity.Metadata.Add("ContentLanguage", googleObject.ContentLanguage);
        entity.Metadata.Add("Crc32", googleObject.Crc32c);

        entity.Metadata.Add("ETag", googleObject.ETag);

        if (googleObject.EventBasedHold.HasValue)
            entity.Metadata.Add("EventBasedHold", googleObject.EventBasedHold);

        if (googleObject.Generation.HasValue)
            entity.Metadata.Add("Generation", googleObject.Generation);

        entity.Metadata.Add("Id", googleObject.Id);
        entity.Metadata.Add("KmsKeyName", googleObject.KmsKeyName);
        entity.Metadata.Add("MediaLink", googleObject.MediaLink);

        if (googleObject.Metageneration.HasValue)
            entity.Metadata.Add("MetaGeneration", googleObject.Metageneration);

        entity.Metadata.Add("Owner", googleObject.Owner);

        if (googleObject.RetentionExpirationTimeDateTimeOffset.HasValue)
            entity.Metadata.Add("RetentionExpirationTime", googleObject.RetentionExpirationTimeDateTimeOffset);

        entity.Metadata.Add("StorageClass", googleObject.StorageClass);

        if (googleObject.TemporaryHold.HasValue)
            entity.Metadata.Add("TemporaryHold", googleObject.TemporaryHold);

        if (googleObject.TimeCreatedDateTimeOffset.HasValue)
            entity.Metadata.Add("TimeCreated", googleObject.TimeCreatedDateTimeOffset);

        if (googleObject.TimeDeletedDateTimeOffset.HasValue)
            entity.Metadata.Add("TimeDeleted", googleObject.TimeDeletedDateTimeOffset);

        if (googleObject.TimeStorageClassUpdatedDateTimeOffset.HasValue)
            entity.Metadata.Add("TimeStorageClassUpdated", googleObject.TimeStorageClassUpdatedDateTimeOffset);
    }
}