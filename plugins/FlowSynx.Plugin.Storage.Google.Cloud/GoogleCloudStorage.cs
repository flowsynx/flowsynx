using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using FlowSynx.IO;
using Google;
using System.Net;
using System.Text;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Abstractions.Models;
using FlowSynx.Plugin.Storage.Abstractions.Options;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

public class GoogleCloudStorage : IStoragePlugin
{
    private readonly ILogger<GoogleCloudStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly ISerializer _serializer;
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
    public string Name => "Google.Cloud";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public Dictionary<string, string?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(GoogleCloudStorageSpecifications);

    public Task Initialize()
    {
        _cloudStorageSpecifications = Specifications.DictionaryToObject<GoogleCloudStorageSpecifications>();
        _client = CreateClient(_cloudStorageSpecifications);
        return Task.CompletedTask;
    }

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
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var result = new List<StorageEntity>();
        var buckets = new List<string>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            // list all of the containers
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            result.AddRange(buckets.Select(b=> b.ToEntity(metadataOptions.IncludeMetadata)));

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
            ListAsync(b, result, path, searchOptions, listOptions, metadataOptions, cancellationToken))
        ).ConfigureAwait(false);

        return _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);
    }
    
    public async Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
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
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && writeOptions.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _client.UploadObjectAsync(pathParts.BucketName, pathParts.RelativePath, null, dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }
    
    public async Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var ms = new MemoryStream();
            var response = await _client.DownloadObjectAsync(pathParts.BucketName, pathParts.RelativePath, ms, cancellationToken: cancellationToken).ConfigureAwait(false);
            var fileExtension = Path.GetExtension(path);

            ms.Position = 0;

            return new StorageRead()
            {
                Stream = new StorageStream(ms),
                ContentType = response.ContentType,
                Extension = fileExtension,
                Md5 = Convert.FromBase64String(response.Md5Hash).ToHexString(),
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetPartsAsync(path);
            return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
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

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            await _client.DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        if (_cloudStorageSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var pathParts = GetPartsAsync(path);
        var isExist = await BucketExists(pathParts.BucketName, cancellationToken);
        if (!isExist)
        {
            await _client.CreateBucketAsync(_cloudStorageSpecifications.ProjectId, pathParts.BucketName,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            await AddFolder(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
    }
    
    public async Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await BucketExists(pathParts.BucketName, cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var searchOptions = new StorageSearchOptions();
            await DeleteAsync(path, searchOptions, cancellationToken);
            await _client.DeleteBucketAsync(pathParts.BucketName, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += "/";

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        try
        {
            var pathParts = GetPartsAsync(path);
            return await ObjectExists(pathParts.BucketName, string.Empty, cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
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

    private async Task ListAsync(string bucketName, List<StorageEntity> result, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions, 
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        using var browser = new GoogleBucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListFolderAsync(path, searchOptions, listOptions, metadataOptions, cancellationToken
        ).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        if (_cloudStorageSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var result = new List<string>();

        await foreach (var bucket in _client.ListBucketsAsync(_cloudStorageSpecifications.ProjectId).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(bucket.Name))
                result.Add(bucket.Name);
        }

        return result;
    }
    
    private async Task<bool> ObjectExists(string bucketName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetObjectAsync(bucketName, fileName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetBucketAsync(bucketName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith("/"))
            folderName += "/";

        var content = Encoding.UTF8.GetBytes("");
        await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory", 
            new MemoryStream(content), cancellationToken: cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    }
    #endregion
}