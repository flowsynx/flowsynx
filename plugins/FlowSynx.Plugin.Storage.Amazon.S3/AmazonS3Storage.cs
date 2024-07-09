using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Amazon.S3;

public class AmazonS3Storage : IStoragePlugin
{
    private readonly ILogger<AmazonS3Storage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly ISerializer _serializer;
    private Dictionary<string, string?>? _specifications;
    private AmazonS3StorageSpecifications? _s3StorageSpecifications;

    public AmazonS3Storage(ILogger<AmazonS3Storage> logger, IStorageFilter storageFilter, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
        _serializer = serializer;
    }

    public Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public string Name => "Amazon.S3.Storage";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public Dictionary<string, string?>? Specifications
    {
        get => _specifications;
        set
        {
            _specifications = value;
            _s3StorageSpecifications = value.DictionaryToObject<AmazonS3StorageSpecifications>();
        }
    }

    public Type SpecificationsType => typeof(AmazonS3StorageSpecifications);

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
}