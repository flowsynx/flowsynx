using FlowSynx.IO;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace FlowSynx.Plugin.Storage.Google.Drive;

static class GoogleDriveConverter
{
    public static StorageEntity ToEntity(this DriveFile file, bool isDirectory, 
        bool? includeMetadata)
    {
        StorageEntity entity;
        var fullPath = PathHelper.Combine(file.Name);

        if (isDirectory)
        {
            entity = new StorageEntity(fullPath, StorageEntityItemKind.Directory)
            {
                ModifiedTime = file.ModifiedTimeDateTimeOffset,
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
                ModifiedTime = file.ModifiedTimeDateTimeOffset,
                Md5 = file.Md5Checksum,
                Size = file.Size
            };
        }

        return entity;
    }
}