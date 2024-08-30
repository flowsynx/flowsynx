using EnsureThat;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Filters;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryStorage : IPlugin
{
    private readonly ILogger<MemoryStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly Dictionary<string, Dictionary<string, MemoryEntity>> _entities;

    public MemoryStorage(ILogger<MemoryStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
        _entities = new Dictionary<string, Dictionary<string, MemoryEntity>>();
    }
    
    public Guid Id => Guid.Parse("ac220180-021e-4150-b0e1-c4d4bdbfb9f0");
    public string Name => "Memory";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(MemoryStorageSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    public Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutFilters = filters.ToObject<AboutFilters>();
        long totalSpace = 0, usedSpace = 0, freeSpace = 0;
        try
        {
            var client = new MemoryMetricsClient();
            var metrics = client.GetMetrics();

            totalSpace = metrics.Total;
            usedSpace = metrics.Used;
            freeSpace = metrics.Free;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            usedSpace = 0;
            freeSpace = 0;
        }

        return Task.FromResult<object>(new
        {
            Total = totalSpace.ToString(!aboutFilters.Full), 
            Free = freeSpace.ToString(!aboutFilters.Full), 
            Used = usedSpace.ToString(!aboutFilters.Full)
        });
    }

    public async Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = GetPartsAsync(path);
        var isBucketExist = BucketExists(pathParts.BucketName);
        if (!isBucketExist)
        {
            _entities.Add(pathParts.BucketName, new Dictionary<string, MemoryEntity>());
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        var entityId = string.Empty;
        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
            if (!isExist)
            {
                entityId = await AddFolder(pathParts.BucketName, pathParts.RelativePath).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation($"Directory '{pathParts.RelativePath}' is already exist.");
                var bucket = _entities[pathParts.BucketName];
                entityId = bucket[pathParts.RelativePath].Id;
            }
        }
        
        return new { entityId };
    }

    public Task<object> WriteAsync(string entity, PluginFilters? filters, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeFilters = filters.ToObject<WriteFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (dataOptions is not Stream dataStream)
            throw new StorageException(nameof(dataStream));

        var pathParts = GetPartsAsync(path);
        var isBucketExist = BucketExists(pathParts.BucketName);
        if (!isBucketExist)
        {
            _entities.Add(pathParts.BucketName, new Dictionary<string, MemoryEntity>());
        }

        var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
        if (isExist && writeFilters.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var name = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = new MemoryEntity(name, dataStream);
        bucket[pathParts.RelativePath] = memoryEntity;
        
        return Task.FromResult<object>(new { memoryEntity.Id });
    }

    public Task<object> ReadAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readFilters = filters.ToObject<ReadFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var pathParts = GetPartsAsync(path);
        var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);

        if (!isExist)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = bucket[pathParts.RelativePath];
        var extension = Path.GetExtension(path);

        var result = new StorageRead
        {
            Stream = new StorageStream(memoryEntity.Content),
            ContentType = memoryEntity.ContentType,
            Extension = extension,
            Md5 = memoryEntity.Md5,
        };

        return Task.FromResult<object>(result);
    }

    public Task<object> UpdateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<object>> DeleteAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteFilters = filters.ToObject<DeleteFilters>();
        var entities = await ListAsync(path, filters, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var result = new List<string>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list)
                continue;

            if (DeleteEntityAsync(list.Path))
            {
                result.Add(list.Id);
            }
        }

        if (deleteFilters.Purge is true)
        {
            var pathParts = GetPartsAsync(path);
            if (!string.IsNullOrEmpty(pathParts.RelativePath))
            {
                var bucket = _entities[pathParts.BucketName];
                var itemToDelete = bucket.Keys.Where(x => x.StartsWith(pathParts.RelativePath)).Select(p => p);

                var toDelete = itemToDelete.ToList();
                if (toDelete.Any())
                {
                    foreach (var item in toDelete)
                    {
                        bucket.Remove(item);
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(pathParts.RelativePath) || PathHelper.IsRootPath(pathParts.RelativePath))
                    _entities.Remove(pathParts.BucketName);
            }
        }

        return result;
    }

    public Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
        if (PathHelper.IsFile(path))
        {
            return Task.FromResult(ObjectExists(pathParts.BucketName, pathParts.RelativePath));
        }

        var folderExist = FolderExistAsync(pathParts.BucketName, pathParts.RelativePath);
        return Task.FromResult(folderExist);
    }

    public async Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();
        var listFilters = filters.ToObject<ListFilters>();

        var pathParts = GetPartsAsync(path);

        if (pathParts.BucketName == "")
        {
            if (pathParts.RelativePath != "")
                throw new StorageException(Resources.BucketNameIsRequired);

            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listFilters.IncludeMetadata)));
            return storageEntities;
        }

        buckets.Add(pathParts.BucketName);

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(storageEntities, b, pathParts.RelativePath, listFilters, cancellationToken))
        ).ConfigureAwait(false);

        var filteredEntities = _storageFilter.Filter(storageEntities, filters).ToList();

        var result = new List<StorageList>(filteredEntities.Count());
        result.AddRange(filteredEntities.Select(storageEntity => new StorageList
        {
            Id = storageEntity.Id,
            Kind = storageEntity.Kind.ToString().ToLower(),
            Name = storageEntity.Name,
            Path = storageEntity.FullPath,
            CreatedTime = storageEntity.CreatedTime,
            ModifiedTime = storageEntity.ModifiedTime,
            Size = storageEntity.Size.ToString(!listFilters.Full),
            ContentType = storageEntity.ContentType,
            Md5 = storageEntity.Md5,
            Metadata = storageEntity.Metadata
        }));

        return result;
    }

    public async Task<object> CompressAsync(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, filters, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList entry)
            {
                _logger.LogWarning("The item is not valid object type. It should be StorageEntity type.");
                continue;
            }

            if (!string.Equals(entry.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entry.Name}' is not a file.");
                continue;
            }

            var stream = await ReadAsync(entry.Path, filters, cancellationToken);
            if (stream is not StorageRead storageRead)
            {
                _logger.LogWarning($"The item '{entry.Name}' could be not read.");
                continue;
            }

            compressEntries.Add(new CompressEntry
            {
                Name = entry.Name,
                ContentType = entry.ContentType,
                Stream = storageRead.Stream
            });
        }

        return compressEntries;
    }

    public void Dispose() { }

    #region private methods
    private Task<List<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        var buckets = _entities.Keys;
        var result = buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket))
            .Select(bucket => bucket).ToList();

        return Task.FromResult(result);
    }

    private Task ListAsync(List<StorageEntity> result, string bucketName, string path,
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        if (!_entities.ContainsKey(bucketName))
            throw new Exception(string.Format(Resources.BucketNotExist, bucketName));

        var bucket = _entities[bucketName];

        foreach (var (key, value) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memEntity = bucket[key];
                result.Add(memEntity.ToEntity(bucketName, listFilters.IncludeMetadata));
            }
        }

        return Task.CompletedTask;
    }

    private bool ObjectExists(string bucketName, string path)
    {
        try
        {
            if (!BucketExists(bucketName))
                return false;

            var bucket = _entities[bucketName];
            return bucket.ContainsKey(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool FolderExistAsync(string bucketName, string path)
    {
        if (!BucketExists(bucketName))
            return false;

        var bucket = _entities[bucketName];
        var folderPrefix = path + PathHelper.PathSeparator;
        return bucket.Keys.Any(x=>x.StartsWith(folderPrefix));
    }

    private bool BucketExists(string bucketName)
    {
        try
        {
            return _entities.ContainsKey(bucketName);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private MemoryStorageBucketPathPart GetPartsAsync(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return new MemoryStorageBucketPathPart();

        string bucketName, relativePath;
        fullPath = PathHelper.Normalize(fullPath);
        var symbolIndex = fullPath.IndexOf('/');

        if (symbolIndex < 0)
        {
            bucketName = fullPath;
            relativePath = string.Empty;
        }
        else
        {
            bucketName = fullPath[..(symbolIndex)];
            relativePath = fullPath[(symbolIndex + 1)..];
        }

        return new MemoryStorageBucketPathPart(bucketName, relativePath);
    }


    private Task<string> AddFolder(string bucketName, string folderName)
    {
        var memoryEntity = new MemoryEntity(folderName);
        var bucket = _entities[bucketName];

        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        bucket[folderName] = memoryEntity;
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
        return Task.FromResult<string>(memoryEntity.Id);
    }

    private bool DeleteEntityAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
        var bucket = _entities[pathParts.BucketName];

        if (PathHelper.IsFile(path))
        {
            var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
            if (!isExist)
                return false;

            var ms = bucket[pathParts.RelativePath];

            bucket.Remove(pathParts.RelativePath);
            return true;
        }

        var folderPrefix = !pathParts.RelativePath.EndsWith(PathHelper.PathSeparator) 
            ? pathParts.RelativePath + PathHelper.PathSeparator 
            : pathParts.RelativePath;

        var folderExist = bucket.Keys.Any(x => x.StartsWith(folderPrefix));

        if (!folderExist)
            return false;

        var itemToDelete = bucket.Keys.Where(x => x.StartsWith(folderPrefix)).Select(p=>p);
        foreach (var item in itemToDelete)
        {
            bucket.Remove(item);
        }
        return true;
    }
    #endregion
}