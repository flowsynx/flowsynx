using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Models;
using FlowSync.Storage.Local.Extensions;
using FlowSync.Abstractions;
using FlowSync.Abstractions.Filter;
using Microsoft.Extensions.Logging;
using EnsureThat;

namespace FlowSync.Storage.Local;

public class LocalFileSystem : IFileSystemPlugin
{
    private readonly ILogger<LocalFileSystem> _logger;
    private readonly IFileSystemFilter _fileSystemFilter;
    private IDictionary<string, object>? _specifications;

    public LocalFileSystem(ILogger<LocalFileSystem> logger, IFileSystemFilter fileSystemFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(fileSystemFilter, nameof(fileSystemFilter));
        _logger = logger;
        _fileSystemFilter = fileSystemFilter;
    }

    public Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public string Namespace => "FlowSync.FileSystem/Local";
    public string? Description => null;
    public void SetSpecifications(IDictionary<string, object>? specifications) => _specifications = specifications;

    public Task<Usage> About(CancellationToken cancellationToken = default)
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

        return Task.FromResult(new Usage { Total = totalSpace, Free = freeSpace});
    }

    public Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("The specified path should not be empty!");
                throw new ArgumentNullException(nameof(path), "The specified path should not be empty!");
            }

            path = GetPhysicalPath(path);

            if (!Directory.Exists(path))
            {
                _logger.LogError($"The specified path '{path}' is not exist.");
                throw new ArgumentException($"The specified path '{path}' is not exist.", nameof(path));
            }

            var result = new List<FileSystemEntity>();
            var searchOption = fileSystemFilters.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var directoryInfo = new DirectoryInfo(path);

            if (fileSystemFilters.Kind is FilterItemKind.File or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateFiles("*", searchOption).Select(GetVirtualFilePath));

            if (fileSystemFilters.Kind is FilterItemKind.Directory or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateDirectories("*", searchOption).Select(GetVirtualDirectoryPath));
            
            var filteredResult = _fileSystemFilter.FilterEntitiesList(result, fileSystemFilters);
            return Task.FromResult(filteredResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new Exception(ex.Message);
        }
    }

    public Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        if (dataStream.Length == 0)
        {
            _logger.LogError($"The stream data should not be null.");
            throw new ArgumentException($"The stream data should not be null.", nameof(dataStream));
        }

        path = GetPhysicalPath(path);
        using var fileStream = File.Create(path);
        dataStream.CopyTo(fileStream);
        return Task.CompletedTask;
    }

    public Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (File.Exists(path)) return Task.FromResult(File.OpenRead(path));

        _logger.LogError($"The specified path '{path}' is not a file.");
        throw new ArgumentException($"The specified path '{path}' is not a file.", nameof(path));
    }

    public async Task DeleteAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        var files = await ListAsync(path, fileSystemFilters, cancellationToken);
        var fileSystemEntities = files.ToList();
        if (!fileSystemEntities.Any())
            throw new Exception($"There are no files found to delete!");

        foreach (var file in fileSystemEntities)
        {
            await DeleteFileAsync(file.FullPath, cancellationToken);
        }
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (!File.Exists(path))
        {
            _logger.LogError($"The specified path '{path}' is not a file.");
            throw new ArgumentException($"The specified path '{path}' is not a file.", nameof(path));
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
            throw new ArgumentException("The specified path is already exist.", nameof(path));
        }

        System.IO.Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #region internal methods
    private string GetPhysicalPath(string path) => IsWindows ? path.ToWindowsPath() : path;

    private bool IsWindows => OperatingSystem.IsWindows();

    private FileSystemEntity GetVirtualDirectoryPath(DirectoryInfo directory)
    {
        return new FileSystemEntity(directory.FullName.ToUnixPath(), EntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null,
            Properties = { }
        };
    }

    private FileSystemEntity GetVirtualFilePath(FileInfo file)
    {
        //FileAttributes attributes = File.GetAttributes(path);

        return new FileSystemEntity(file.FullName.ToUnixPath(), EntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length,
            //Properties = file.Attributes
        };
    }
    #endregion
}