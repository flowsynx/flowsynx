using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;

namespace FlowSynx.Plugin.Storage.Memory;

static class MemoryStorageConverter
{
    public static StorageEntity ToEntity(this string bucketName, bool? includeMetadata)
    {
        var entity = new StorageEntity(bucketName, StorageEntityItemKind.Directory)
        {
            Size = 0
        };

        if (includeMetadata is true)
        {
            entity.Metadata["IsBucket"] = true;
        }

        return entity;
    }

    public static StorageEntity ToEntity(this MemoryEntity memoryEntity, string bucketName, bool? includeMetadata)
    {
        var fullPath = PathHelper.Combine(bucketName, memoryEntity.Name);
        var isDirectory = memoryEntity.Name.EndsWith(PathHelper.PathSeparator) && PathHelper.IsDirectory(memoryEntity.FullPath);
        StorageEntity entity = new StorageEntity(fullPath, memoryEntity.Kind);

        if (!isDirectory)
        {
            entity.Size = memoryEntity.Size;
            entity.ModifiedTime = memoryEntity.ModifiedTime;
        }

        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Metadata", memoryEntity.Metadata);
            if (isDirectory)
            {
                entity.Metadata["IsDirectory"] = true;
            }
        }

        return entity;
    }
}