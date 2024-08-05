using EnsureThat;
using FlowSynx.IO;
using FlowSynx.Net;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Security;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;

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
        var result = new List<StorageEntity>();
        var buckets = new List<string>();

        var (bucket, directory) = GetPartsAsync(path);

        if (bucket == "") {
            if (directory != "")
                throw new Exception("ErrorListBucketRequired");

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
                throw new StorageException("Resources.FileIsAlreadyExistAndCannotBeOverwritten");

            var bucket = _entities[bucketName];
            bucket[directory] = new MemoryEntity(dataStream);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageException("ResourceNotExist");
        }
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

        if (!path.EndsWith("/"))
            path += "/";

        var bucket = _entities[bucketName];

        foreach (var (key, value) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memEntity = bucket[key];
                result.Add(memEntity.ToEntity(bucketName, key, false));
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
            : (path[..(symbolIndex)], path[(symbolIndex)..]);
    }
    #endregion
}