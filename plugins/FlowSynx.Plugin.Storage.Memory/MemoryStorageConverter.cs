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

    public static StorageEntity ToEntity(this MemoryEntity memoryEntity, string name, bool? includeMetadata)
    {
        var entity = new StorageEntity(name, StorageEntityItemKind.File);
        return entity;
    }
}