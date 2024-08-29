using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;

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

    public static StorageEntity ToEntity(this MemoryEntity memoryEntity, string bucketName, bool? includeMetadata)
    {
        var fullPath = PathHelper.Combine(bucketName, memoryEntity.Name);
        var isDirectory = memoryEntity.Name.EndsWith(PathHelper.PathSeparator) && memoryEntity.IsDirectory;
        StorageEntity entity = new StorageEntity(fullPath, memoryEntity.Kind);

        if (!isDirectory)
        {
            entity.Size = memoryEntity.Size;
            entity.Md5 = memoryEntity.Md5;
            entity.ModifiedTime = memoryEntity.ModifiedTime;
        }

        if (includeMetadata is true)
        {
            if (isDirectory)
            {
                entity.Metadata["IsDirectory"] = true;
            }
        }

        return entity;
    }
}