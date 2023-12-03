using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace FlowSync.Storage.Azure.Blob;

public class AzureBlobStorage : IStoragePlugin
{
    private readonly ILogger<AzureBlobStorage> _logger;

    public AzureBlobStorage(ILogger<AzureBlobStorage> logger)
    {
        _logger = logger;
    }

    public Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public string Name => "Azure.Blob";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => null;
    public Specifications? Specifications { get; set; }
    
    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions? filters, int? maxResult, CancellationToken cancellationToken = default)
    {
        ICollection<StorageEntity> result = new List<StorageEntity>() { };
        return Task.FromResult(result.AsEnumerable());
    }

    public Task WriteAsync(string path, StorageStream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StorageStream> ReadAsync(string path, CancellationToken cancellationToken = default)
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
}