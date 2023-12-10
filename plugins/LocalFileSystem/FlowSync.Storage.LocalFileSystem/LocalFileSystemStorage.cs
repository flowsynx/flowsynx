﻿using FlowSync.Storage.Local.Extensions;
using FlowSync.Abstractions;
using FlowSync.Abstractions.Exceptions;
using FlowSync.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using EnsureThat;

namespace FlowSync.Storage.LocalFileSystem;

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
    public string? Description => null;
    public Specifications? Specifications { get; set; }

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

        return Task.FromResult(new StorageUsage { Total = totalSpace, Free = freeSpace});
    }

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions, int? maxResult, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("The specified path should not be empty!");
                throw new StorageException("The specified path should not be empty!");
            }

            path = GetPhysicalPath(path);

            if (!Directory.Exists(path))
            {
                _logger.LogError($"The specified path '{path}' is not exist.");
                throw new StorageException($"The specified path '{path}' is not exist.");
            }

            var result = new List<StorageEntity>();
            var directoryInfo = new DirectoryInfo(path);

            if (searchOptions.Kind is StorageFilterItemKind.File or StorageFilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.FindFiles("*", searchOptions.Recurse).Select(GetVirtualFilePath));

            if (searchOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.FindDirectories("*", searchOptions.Recurse).Select(GetVirtualDirectoryPath));
            
            var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions);

            if (maxResult is > 0)
                filteredResult = filteredResult.Take(maxResult.Value);

            return Task.FromResult(filteredResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageException(ex.Message);
        }
    }

    public Task WriteAsync(string path, StorageStream storageStream, bool append = false, CancellationToken cancellationToken = default)
    {
        if (storageStream.Length == 0)
        {
            _logger.LogError($"The stream data should not be null.");
            throw new StorageException($"The stream data should not be null.");
        }

        path = GetPhysicalPath(path);
        using var fileStream = File.Create(path);
        storageStream.CopyTo(fileStream);
        return Task.CompletedTask;
    }

    public Task<StorageStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (File.Exists(path))
        {
            var stream = new StorageStream(File.OpenRead(path));
            return Task.FromResult(stream);
        }

        _logger.LogError($"The specified path '{path}' is not a file.");
        throw new StorageException($"The specified path '{path}' is not a file.");
    }
    
    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var files = await ListAsync(path, storageSearches, null, cancellationToken);
        var entities = files.ToList();
        if (!entities.Any())
            throw new StorageException($"There are no files found to delete!");

        foreach (var entity in entities)
        {
            await DeleteFileAsync(entity.FullPath, cancellationToken);
        }
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (!File.Exists(path))
        {
            _logger.LogError($"The specified path '{path}' is not a file.");
            throw new StorageException($"The specified path '{path}' is not a file.");
        }

        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (Directory.Exists(path))
        {
            _logger.LogError("The specified path is already exist.");
            throw new StorageException("The specified path is already exist.");
        }

        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }

    #region internal methods
    private string GetPhysicalPath(string path) => IsWindows ? path.ToWindowsPath() : path;

    private bool IsWindows => OperatingSystem.IsWindows();

    private StorageEntity GetVirtualDirectoryPath(DirectoryInfo directory)
    {
        return new StorageEntity(directory.FullName.ToUnixPath(), StorageEntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null
        };
    }

    private StorageEntity GetVirtualFilePath(FileInfo file)
    {
        return new StorageEntity(file.FullName.ToUnixPath(), StorageEntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length
        };
    }
    #endregion
}