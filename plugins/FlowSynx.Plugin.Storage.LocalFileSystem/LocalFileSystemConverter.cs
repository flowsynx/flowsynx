using FlowSynx.Security;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

static class LocalFileSystemConverter
{
    public static StorageEntity ToEntity(DirectoryInfo directory)
    {
        return new StorageEntity(directory.FullName.ToUnixPath(), StorageEntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null
        };
    }

    public static StorageEntity ToEntity(FileInfo file, bool? hashing)
    {
        return new StorageEntity(file.FullName.ToUnixPath(), StorageEntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length,
            Md5 = hashing is true ? HashHelper.GetMd5HashFile(file.FullName) : null,
        };
    }
}