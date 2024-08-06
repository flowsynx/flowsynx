using EnsureThat;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryStorage : IStoragePlugin
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
    public Dictionary<string, string?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(MemoryStorageSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
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

        return Task.FromResult(new StorageUsage { Total = totalSpace, Free = freeSpace, Used = usedSpace });
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);
        
        var result = new List<StorageEntity>();
        var buckets = new List<string>();

        var (bucket, directory) = GetPartsAsync(path);

        if (bucket == "") {
            if (directory != "")
                throw new StorageException(Resources.BucketNameIsRequired);

            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            result.AddRange(buckets.Select(b => b.ToEntity(metadataOptions.IncludeMetadata)));
            return result;
        }
        
        buckets.Add(bucket);

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(result, b, directory, searchOptions, listOptions, metadataOptions, cancellationToken))
        ).ConfigureAwait(false);

        return _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);
    }
    
    public Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (dataStream == null)
            throw new ArgumentNullException(nameof(dataStream));

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var (bucketName, directory) = GetPartsAsync(path);
            var isBucketExist = BucketExists(bucketName);
            if (!isBucketExist)
            {
                _entities.Add(bucketName, new Dictionary<string, MemoryEntity>());
            }

            var isExist = ObjectExists(bucketName, directory);
            if (isExist && writeOptions.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var name = Path.GetFileName(path);
            var extension = Path.GetExtension(path);
            var bucket = _entities[bucketName];
            bucket[directory] = new MemoryEntity(name, dataStream);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var (bucketName, directory) = GetPartsAsync(path);
            var isExist = ObjectExists(bucketName, directory);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var bucket = _entities[bucketName];
            var memoryEntity = bucket[directory];
            var extension = Path.GetExtension(path);
            
            var response = new StorageRead()
            {
                Stream = new StorageStream(memoryEntity.Content),
                ContentType = memoryEntity.ContentType,
                Extension = extension,
                Md5 = memoryEntity.Md5,
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var (bucketName, directory) = GetPartsAsync(path);
            return Task.FromResult(ObjectExists(bucketName, directory));
        }
        catch (Exception ex)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var listOptions = new StorageListOptions { Kind = StorageFilterItemKind.File };
        var hashOptions = new StorageHashOptions() { Hashing = false };
        var metadataOptions = new StorageMetadataOptions() { IncludeMetadata = false };

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
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var (bucketName, directory) = GetPartsAsync(path);
            var isExist = ObjectExists(bucketName, directory);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var bucket = _entities[bucketName];
            var ms = bucket[directory];

            bucket.Remove(directory);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var (bucketName, directory) = GetPartsAsync(path);
        var isBucketExist = BucketExists(bucketName);
        if (!isBucketExist)
        {
            _entities.Add(bucketName, new Dictionary<string, MemoryEntity>());
            _logger.LogInformation($"Bucket '{bucketName}' was created successfully.");
        }
        
        if (!string.IsNullOrEmpty(directory))
        {
            var isExist = ObjectExists(bucketName, directory);
            if (!isExist)
                AddFolder(bucketName, directory).ConfigureAwait(false);
            else
                _logger.LogInformation($"Directory '{directory}' is already exist.");
        }

        return Task.CompletedTask;
    }

    public Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        if (!_entities.ContainsKey(bucketName))
            throw new Exception($"The bucket '{bucketName}' is not exist");

        var bucket = _entities[bucketName];

        foreach (var (key, value) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memEntity = bucket[key];
                result.Add(memEntity.ToEntity(bucketName, metadataOptions.IncludeMetadata));
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
        catch (Exception ex)
        {
            return false;
        }
    }

    private bool BucketExists(string bucketName)
    {
        try
        {
            return _entities.ContainsKey(bucketName);
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private (string, string) GetPartsAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            return ("", "");

        var symbolIndex = path.IndexOf('/');

        return symbolIndex < 0
            ? (path, "")
            : (path[..(symbolIndex)], path[(symbolIndex+1)..]);
    }

    private Task AddFolder(string bucketName, string folderName)
    {
        if (!folderName.EndsWith("/"))
            folderName += "/";
        
        var memoryEntity = new MemoryEntity(folderName);
        var bucket = _entities[bucketName];
        bucket[folderName] = memoryEntity;
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
        return Task.CompletedTask;
    }
    #endregion
}