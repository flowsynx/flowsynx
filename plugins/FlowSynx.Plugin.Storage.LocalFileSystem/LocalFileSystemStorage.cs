using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.IO;
using FlowSynx.Net;
using FlowSynx.Plugin.Abstractions;
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
    public string? Description => "Plugin for local file system management. Local paths are considered as normal file system paths, e.g. /path/to/wherever";
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

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions, 
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
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

            if (listOptions.Kind is StorageFilterItemKind.File or StorageFilterItemKind.FileAndDirectory)
            {
                result.AddRange(directoryInfo.FindFiles("*", searchOptions.Recurse)
                      .Select(file => LocalFileSystemConverter.ToEntity(file, hashOptions.Hashing)));
            }

            if (listOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory)
            {
                result.AddRange(directoryInfo.FindDirectories("*", searchOptions.Recurse)
                      .Select(LocalFileSystemConverter.ToEntity));
            }

            var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

            if (listOptions.MaxResult is > 0)
                filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

            return Task.FromResult(filteredResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageException(ex.Message);
        }
    }

    public Task WriteAsync(string path, StorageStream storageStream, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);
        using var fileStream = !File.Exists(path) ? File.Create(path) : File.Open(path, FileMode.Append);
        storageStream.CopyTo(fileStream);
        return Task.CompletedTask;
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions, 
        CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (File.Exists(path))
        {
            var fileExtension = Path.GetExtension(path);
            var result = new StorageRead()
            {
                Stream = new StorageStream(File.OpenRead(path)),
                MimeType = fileExtension.GetMimeType(),
                Extension = fileExtension,
                Md5 = HashHelper.GetMd5HashFile(path)
            };
            return Task.FromResult(result);
        }

        _logger.LogError($"The specified path '{path}' is not a file.");
        throw new StorageException($"The specified path '{path}' is not a file.");
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            _logger.LogError("The specified path should not be empty!");
            throw new StorageException("The specified path should not be empty!");
        }

        var fileInfo = new FileInfo(path);
        return Task.FromResult(fileInfo.Exists);
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(path, storageSearches,
            new StorageListOptions(), new StorageHashOptions(), cancellationToken);

        foreach (var entity in entities.Where(x=>x.IsFile).ToList())
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

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
        {
            _logger.LogError("The specified directory path is not exist.");
            throw new StorageException("The specified directory path is not exist.");
        }

        Directory.Delete(path, true);
        return Task.CompletedTask;
    }

    public Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            _logger.LogError("The specified path should not be empty!");
            throw new StorageException("The specified path should not be empty!");
        }

        var fileInfo = new DirectoryInfo(path);
        return Task.FromResult(fileInfo.Exists);
    }

    public void Dispose() { }

    #region internal methods
    private string GetPhysicalPath(string path) => IsWindows ? path.ToWindowsPath() : path;

    private bool IsWindows => OperatingSystem.IsWindows();
    #endregion
}