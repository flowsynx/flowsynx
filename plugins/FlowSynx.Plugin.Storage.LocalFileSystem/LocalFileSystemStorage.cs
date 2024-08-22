using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Net;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Security;
using FlowSynx.Commons;
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

public class LocalFileSystemStorage : IPlugin
{
    private readonly ILogger<LocalFileSystemStorage> _logger;
    private readonly IStorageFilter _storageFilter;

    public LocalFileSystemStorage(ILogger<LocalFileSystemStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public string Name => "LocalFileSystem";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }
    
    public Task<object> About(PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutFilters = filters.ToObject<AboutFilters>();
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

        var result = new StorageUsage
        {
            Total = totalSpace.ToString(!aboutFilters.Full),
            Free = freeSpace.ToString(!aboutFilters.Full),
            Used = (totalSpace - freeSpace).ToString(!aboutFilters.Full)
        };
        return Task.FromResult<object>(result);
    }

    public Task<object> CreateAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createFilters = filters.ToObject<CreateFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (createFilters.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        var result = new StorageEntity(path, StorageEntityItemKind.Directory);
        return Task.FromResult<object>(new { result.Id });
    }

    public Task<object> WriteAsync(string entity, PluginFilters? filters, 
        object dataOptions, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeFilters = filters.ToObject<WriteFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && writeFilters.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        if (dataOptions is not Stream dataStream)
            throw new StorageException(nameof(dataStream));

        if (File.Exists(path) && writeFilters.Overwrite is true)
            DeleteEntityAsync(path);
        
        using var fileStream = File.Create(path);
        dataStream.CopyTo(fileStream);
        var result = new StorageEntity(path, StorageEntityItemKind.File);
        return Task.FromResult<object>(new { result.Id });
    }

    public Task<object> ReadAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readFilters = filters.ToObject<ReadFilters>();

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

        return Task.FromResult<object>(result);
    }

    public Task<object> UpdateAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> DeleteAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistAsync(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var fileInfo = new FileInfo(path);
        return Task.FromResult<bool>(fileInfo.Exists);
    }

    public Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);
        
        if (!Directory.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var listFilters = filters.ToObject<ListFilters>();

        if (!string.IsNullOrEmpty(listFilters.Kind) && !EnumUtils.TryParseWithMemberName<StorageEntityItemKind>(listFilters.Kind, out _))
            throw new StorageException(Resources.ListValidatorKindValueMustBeValidMessage);
        
        var storageEntities = new List<StorageEntity>();
        var directoryInfo = new DirectoryInfo(path);

        storageEntities.AddRange(directoryInfo.FindFiles("*", listFilters.Recurse)
            .Select(file => file.ToEntity(listFilters.Hashing, listFilters.IncludeMetadata)));

        storageEntities.AddRange(directoryInfo.FindDirectories("*", listFilters.Recurse)
            .Select(dir => dir.ToEntity(listFilters.IncludeMetadata)));

        var filteredEntities = _storageFilter.Filter(storageEntities, filters).ToList();

        var result = new List<StorageList>(filteredEntities.Count());
        result.AddRange(filteredEntities.Select(storageEntity => new StorageList
        {
            Id = storageEntity.Id,
            Kind = storageEntity.Kind.ToString().ToLower(),
            Name = storageEntity.Name,
            Path = storageEntity.FullPath,
            ModifiedTime = storageEntity.ModifiedTime,
            Size = storageEntity.Size.ToString(!listFilters.Full),
            ContentType = storageEntity.ContentType,
            Md5 = storageEntity.Md5,
            Metadata = storageEntity.Metadata
        }));

        return Task.FromResult<IEnumerable<object>>(result);
    }

    #region internal methods

    private void DeleteEntityAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotFile, path));

        File.Delete(path);
    }

    #endregion
    public void Dispose() { }
}