using FlowSynx.IO;

namespace FlowSynx.Plugin.Storage.Memory;

static class MemoryStorageConverter
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

    public static StorageEntity ToEntity(this MemoryEntity memoryEntity, string bucketName, string name, bool? includeMetadata)
    {
        var fullPath = PathHelper.Combine(bucketName, name);
        var entity = new StorageEntity(fullPath, StorageEntityItemKind.File);
        return entity;
    }
}