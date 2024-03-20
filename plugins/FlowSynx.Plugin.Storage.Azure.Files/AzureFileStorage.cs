using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFileStorage : IStoragePlugin
{
    private readonly ILogger<AzureFileStorage> _logger;
    private readonly IStorageFilter _storageFilter;

    public AzureFileStorage(ILogger<AzureFileStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d");
    public string Name => "Azure.Files";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => "Plugin for managing Microsoft Azure File storage system.";
    public Specifications? Specifications { get; set; }

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task WriteAsync(string path, StorageStream storageStream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
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
}