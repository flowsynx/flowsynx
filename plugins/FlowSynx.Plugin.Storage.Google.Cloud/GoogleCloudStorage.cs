using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using FlowSynx.IO;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

public class GoogleCloudStorage : IStoragePlugin
{
    private readonly ILogger<GoogleCloudStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly ISerializer _serializer;
    private Dictionary<string, string?>? _specifications;
    private GoogleCloudStorageSpecifications? _cloudStorageSpecifications;
    private StorageClient _client = null!;

    public GoogleCloudStorage(ILogger<GoogleCloudStorage> logger, IStorageFilter storageFilter, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
        _serializer = serializer;
    }

    public Guid Id => Guid.Parse("d3c52770-f001-4ea3-93b7-f113a956a091");
    public string Name => "Google.Cloud.Storage";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public Dictionary<string, string?>? Specifications
    {
        get => _specifications;
        set
        {
            _specifications = value;
            _cloudStorageSpecifications = value.DictionaryToObject<GoogleCloudStorageSpecifications>();
            _client = CreateClient(_cloudStorageSpecifications);
        }
    }

    public Type SpecificationsType => typeof(GoogleCloudStorageSpecifications);

    private StorageClient CreateClient(GoogleCloudStorageSpecifications specifications)
    {
        var jsonObject = new
        {
            type = specifications.Type,
            project_id = specifications.ProjectId,
            private_key_id = specifications.PrivateKeyId,
            private_key = specifications.PrivateKey,
            client_email = specifications.ClientEmail,
            client_id = specifications.ClientId,
            auth_uri = specifications.AuthUri,
            token_uri = specifications.TokenUri,
            auth_provider_x509_cert_url = specifications.AuthProviderX509CertUrl,
            client_x509_cert_url = specifications.ClientX509CertUrl,
            universe_domain = specifications.UniverseDomain
        };

        var json = _serializer.Serialize(jsonObject);
        var credential = GoogleCredential.FromJson(json);
        return StorageClient.Create(credential);
    }

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        var result = new List<StorageEntity>();
        var buckets = new List<string>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            // list all of the containers
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            result.AddRange(buckets.Select(GoogleCloudStorageConverter.ToEntity));

            if (!searchOptions.Recurse)
                return result;
        }
        else
        {
            var pathParts = GetPartsAsync(path);
            path = pathParts.RelativePath;
            buckets.Add(pathParts.BucketName);
        }

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(b, result, path, searchOptions, listOptions, cancellationToken))
        ).ConfigureAwait(false);

        var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

        if (listOptions.MaxResult is > 0)
            filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

        return filteredResult;
    }

    private async Task ListAsync(string bucketName, List<StorageEntity> result, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions, CancellationToken cancellationToken)
    {
        using var browser = new GoogleBucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListFolderAsync(path, searchOptions, listOptions, cancellationToken).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        if (_cloudStorageSpecifications == null)
            throw new StorageException("Google Cloud Storage Specifications could not be null or empty!");

        var result = new List<string>();

        await foreach (var bucket in _client.ListBucketsAsync(_cloudStorageSpecifications.ProjectId).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(bucket.Name))
                result.Add(bucket.Name);
        }
        
        return result;
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
    private GoogleCloudStorageBucketPathPart GetPartsAsync(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        if (fullPath == null)
            throw new ArgumentNullException(nameof(fullPath));

        string bucketName, relativePath;
        string[] parts = PathHelper.Split(fullPath);

        if (parts.Length == 1)
        {
            bucketName = parts[0];
            relativePath = string.Empty;
        }
        else
        {
            bucketName = parts[0];
            relativePath = PathHelper.Combine(parts.Skip(1));
        }

        return new GoogleCloudStorageBucketPathPart(bucketName, relativePath);
    }

    private string NormalizePath(string path)
    {
        return PathHelper.Normalize(path);
    }

    private async Task<Bucket> GetBucket(string bucketName)
    {
        var bucket = await _client.GetBucketAsync(bucketName);
        return bucket;
    }
    #endregion
}