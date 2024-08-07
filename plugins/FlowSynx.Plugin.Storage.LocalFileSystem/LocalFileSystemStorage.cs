﻿using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.IO;
using FlowSynx.Net;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Abstractions.Models;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Security;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

public class LocalFileSystemStorage : IStoragePlugin
{
    private readonly ILogger<LocalFileSystemStorage> _logger;
    private readonly IStorageFilter _storageFilter;

    public LocalFileSystemStorage(ILogger<LocalFileSystemStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public string Name => "LocalFileSystem";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public Dictionary<string, string?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        long totalSpace = 0, freeSpace = 0;
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d is not { DriveType: DriveType.Fixed, IsReady: true }) continue;

                totalSpace += d.TotalSize;
                freeSpace += d.TotalFreeSpace;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            freeSpace = 0;
        }

        return Task.FromResult(new StorageUsage { Total = totalSpace, Free = freeSpace, Used = totalSpace - freeSpace});
    }

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions, 
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        try
        {
            if (!Directory.Exists(path))
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var result = new List<StorageEntity>();
            var directoryInfo = new DirectoryInfo(path);

            result.AddRange(directoryInfo.FindFiles("*", searchOptions.Recurse)
                      .Select(file => file.ToEntity(hashOptions.Hashing, metadataOptions.IncludeMetadata)));

            result.AddRange(directoryInfo.FindDirectories("*", searchOptions.Recurse)
                      .Select(dir=> dir.ToEntity(metadataOptions.IncludeMetadata)));

            var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);
            return Task.FromResult(filteredResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageException(ex.Message);
        }
    }

    public Task WriteAsync(string path, StorageStream storageStream, StorageWriteOptions writeOptions, 
        CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);
        
        if (File.Exists(path) && writeOptions.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));
        
        if (File.Exists(path) && writeOptions.Overwrite is true)
        {
            DeleteFileAsync(path, cancellationToken);
        }
        using var fileStream = File.Create(path);
        storageStream.CopyTo(fileStream);
        return Task.CompletedTask;
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions, 
        CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotFile, path));

        var file = new FileInfo(path);
        var fileExtension = file.Extension;
        var result = new StorageRead()
        {
            Stream = new StorageStream(File.OpenRead(path)),
            ContentType = fileExtension.GetContentType(),
            Extension = fileExtension,
            Md5 = HashHelper.Md5.GetHash(file)
        };

        return Task.FromResult(result);
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var fileInfo = new FileInfo(path);
        return Task.FromResult(fileInfo.Exists);
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var listOptions = new StorageListOptions { Kind = StorageFilterItemKind.File };
        var hashOptions = new StorageHashOptions() { Hashing = false };
        var metadataOptions = new StorageMetadataOptions() { IncludeMetadata = false };

        path = path.ToUnixPath();

        var entities = 
            await ListAsync(path, storageSearches, listOptions, hashOptions, metadataOptions, cancellationToken);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            _logger.LogWarning(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (var entity in storageEntities)
        {
            await DeleteFileAsync(entity.FullPath, cancellationToken);
        }
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotFile, path));
        
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            throw new StorageException(string.Format(Resources.TheSpecifiedDirectoryPathIsNotDirectory, path));
        
        Directory.Delete(path, true);
        return Task.CompletedTask;
    }

    public Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.ToUnixPath();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var fileInfo = new DirectoryInfo(path);
        return Task.FromResult(fileInfo.Exists);
    }

    public void Dispose() { }
}