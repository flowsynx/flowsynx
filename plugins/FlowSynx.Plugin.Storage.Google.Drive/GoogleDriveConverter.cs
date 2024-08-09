﻿using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace FlowSynx.Plugin.Storage.Google.Drive;

static class GoogleDriveConverter
{
    public static StorageEntity ToEntity(this DriveFile file, string path, bool isDirectory, 
        bool? includeMetadata)
    {
        StorageEntity entity;
        var fullPath = PathHelper.Combine(path, file.Name);

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

            if (includeMetadata is true)
            {
                AddProperties(entity, file);
            }
        }

        return entity;
    }

    private static void AddProperties(StorageEntity entity, DriveFile file)
    {
        if (file.CreatedTimeDateTimeOffset.HasValue)
            entity.Metadata.Add("CreatedTimeDateTimeOffset", file.CreatedTimeDateTimeOffset);

        if (file.ModifiedTimeDateTimeOffset.HasValue)
            entity.Metadata.Add("ModifiedTimeDateTime", file.ModifiedTimeDateTimeOffset);
        
        if (file.CopyRequiresWriterPermission.HasValue)
            entity.Metadata.Add("CopyRequiresWriterPermission", file.CopyRequiresWriterPermission);

        if (file.WritersCanShare.HasValue)
            entity.Metadata.Add("WritersCanShare", file.WritersCanShare);
        
        if (file.ViewedByMe.HasValue)
            entity.Metadata.Add("ViewedByMe", file.ViewedByMe);
        
        entity.Metadata.Add("FolderColorRgb", file.FolderColorRgb);

        entity.Metadata.Add("Description", file.Description);

        if (file.Starred.HasValue)
            entity.Metadata.Add("Starred", file.Starred);
    }
}