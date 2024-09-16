using FlowSynx.Security;
using FlowSynx.IO;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

static class LocalFileSystemConverter
{
    public static StorageEntity ToEntity(this DirectoryInfo directory, bool? includeMetadata)
    {
        var entity = new StorageEntity(PathHelper.ToUnixPath(directory.FullName), StorageEntityItemKind.Directory)
        {
            Size = 0,
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Attributes", directory.Attributes.ToString());
        }

        return entity;
    }

    public static StorageEntity ToEntity(this FileInfo file, bool? includeMetadata)
    {
        var fileInfo = new FileInfo(file.FullName);
        var entity = new StorageEntity(PathHelper.ToUnixPath(file.FullName), StorageEntityItemKind.File)
        {
            Size = file.Length,
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc
        };
        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Attributes", file.Attributes.ToString());
            entity.TryAddMetadata("ContentHash", HashHelper.Md5.GetHash(fileInfo));
        }
        return entity;
    }
}