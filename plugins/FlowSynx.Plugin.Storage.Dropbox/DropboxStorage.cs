using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Microsoft.Extensions.Logging;
using Dropbox.Api;

namespace FlowSynx.Plugin.Storage.Dropbox;

public class DropboxStorage : IStoragePlugin
{
    private readonly ILogger<DropboxStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly ISerializer _serializer;
    private DropboxStorageSpecifications? _s3StorageSpecifications;
    private DropboxClient _client = null!;
    
    public DropboxStorage(ILogger<DropboxStorage> logger, IStorageFilter storageFilter, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        _logger = logger;
        _storageFilter = storageFilter;
        _serializer = serializer;
    }

    public Guid Id => Guid.Parse("d22239a9-17b6-40a9-86cb-68f5aca7bc60");
    public string Name => "Dropbox";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public Dictionary<string, string?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(DropboxStorageSpecifications);

    public Task Initialize()
    {
        _s3StorageSpecifications = Specifications.DictionaryToObject<DropboxStorageSpecifications>();
        _client = CreateClient(_s3StorageSpecifications);
        return Task.CompletedTask;
    }

    private DropboxClient CreateClient(DropboxStorageSpecifications specifications)
    {
        return new DropboxClient(specifications.AccessToken);
    }
    
    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        var list = await _client.Files.ListFolderAsync(string.Empty);
        return list.Entries.Select(entity => new StorageEntity(entity.Name, StorageEntityItemKind.File) { }).ToList();
    }
    public async Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }

    #region private methods
    
    #endregion
}