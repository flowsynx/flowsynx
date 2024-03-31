using FlowSynx.Security;
using System.IO;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

static class LocalFileSystemConverter
{
    public static StorageEntity ToEntity(DirectoryInfo directory)
    {
        var entity = new StorageEntity(directory.FullName.ToUnixPath(), StorageEntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null
        };

        entity.TryAddMetadata("Attributes", directory.Attributes.ToString());
        return entity;
    }

    public static StorageEntity ToEntity(FileInfo file, bool? hashing)
    {
        var entity = new StorageEntity(file.FullName.ToUnixPath(), StorageEntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length,
            Md5 = hashing is true ? HashHelper.GetMd5HashFile(file.FullName) : null
        };
        
        entity.TryAddMetadata("Attributes", file.Attributes.ToString());
        return entity;
    }
}