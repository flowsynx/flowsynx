using FlowSynx.Security;
using System.IO;
using FlowSynx.Plugin.Storage.Abstractions;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

static class LocalFileSystemConverter
{
    public static StorageEntity ToEntity(this DirectoryInfo directory, bool? includeMetadata)
    {
        var entity = new StorageEntity(directory.FullName.ToUnixPath(), StorageEntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Attributes", directory.Attributes.ToString());
        }

        return entity;
    }

    public static StorageEntity ToEntity(this FileInfo file, bool? hashing, bool? includeMetadata)
    {
        var fileInfo = new FileInfo(file.FullName);
        var entity = new StorageEntity(file.FullName.ToUnixPath(), StorageEntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length,
            Md5 = hashing is true ? HashHelper.Md5.GetHash(fileInfo) : null
        };
        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Attributes", file.Attributes.ToString());
        }
        return entity;
    }
}