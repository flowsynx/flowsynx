using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

public class AzureBlobStorage : IStoragePlugin
{
    private readonly ILogger<AzureBlobStorage> _logger;

    public AzureBlobStorage(ILogger<AzureBlobStorage> logger)
    {
        _logger = logger;
    }

    public Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public string Name => "Azure.Blobs";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => null;
    public Dictionary<string, object?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(AzureBlobStorageSpecifications);

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions? filters,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        ICollection<StorageEntity> result = new List<StorageEntity>() { };
        return Task.FromResult(result.AsEnumerable());
    }

    public Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
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

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}