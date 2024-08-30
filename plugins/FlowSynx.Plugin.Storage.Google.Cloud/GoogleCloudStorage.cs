using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using FlowSynx.IO;
using Google;
using System.Net;
using System.Text;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

public class GoogleCloudStorage : IPlugin
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
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(GoogleCloudStorageSpecifications);

    public Task Initialize()
    {
        _cloudStorageSpecifications = Specifications.ToObject<GoogleCloudStorageSpecifications>();
        _client = CreateClient(_cloudStorageSpecifications);
        return Task.CompletedTask;
    }

    public Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public async Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createFilters = filters.ToObject<CreateFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

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

        var result = new StorageEntity(path, StorageEntityItemKind.Directory);
        return new { result.Id };
    }

    public async Task<object> WriteAsync(string entity, PluginFilters? filters, object dataOptions,
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

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && writeFilters.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _client.UploadObjectAsync(pathParts.BucketName, pathParts.RelativePath, 
                null, dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var result = new StorageEntity(path, StorageEntityItemKind.File);
            return new { result.Id };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<object> ReadAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readFilters = filters.ToObject<ReadFilters>();

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
            var response = await _client.DownloadObjectAsync(pathParts.BucketName, 
                pathParts.RelativePath, ms, cancellationToken: cancellationToken).ConfigureAwait(false);

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

            if (await DeleteEntityAsync(list.Path, cancellationToken).ConfigureAwait(false))
            {
                result.Add(list.Id);
            }
        }

        if (deleteFilters.Purge is true)
        {
            var pathParts = GetPartsAsync(path);
            await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);

            if (PathHelper.IsFile(path))
                return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
            
            if (PathHelper.IsRootPath(pathParts.RelativePath))
                return await BucketExists(pathParts.BucketName, cancellationToken);
            
            return await FolderExistAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
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

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            // list all of the buckets
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listFilters.IncludeMetadata)));

            if (!listFilters.Recurse)
            {
                var bucketsEntities = new List<StorageList>(storageEntities.Count());
                bucketsEntities.AddRange(storageEntities.Select(storageEntity => new StorageList
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

                return bucketsEntities;
            }
        }
        else
        {
            var pathParts = GetPartsAsync(path);
            path = pathParts.RelativePath;
            buckets.Add(pathParts.BucketName);
        }

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(b, storageEntities, path, listFilters, cancellationToken))
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
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        using var browser = new GoogleBucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListFolderAsync(path, listFilters, cancellationToken
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

    private async Task<bool> FolderExistAsync(string bucketName, string path, CancellationToken cancellationToken)
    {
        var folderPrefix = path + PathHelper.PathSeparator;
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderPrefix;
        request.Delimiter = PathHelper.PathSeparatorString;

        var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (serviceObjects == null)
            return false;

        if (serviceObjects.Items is { Count: > 0 })
            return serviceObjects.Items?.Any(x => x.Name.StartsWith(folderPrefix)) ?? false;
        
        return serviceObjects.Prefixes?.Any(x => x.StartsWith(folderPrefix)) ?? false;
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var content = Encoding.UTF8.GetBytes("");
        await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory", 
            new MemoryStream(content), cancellationToken: cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    }

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            if (PathHelper.IsFile(path))
            {
                var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
                if (!isExist)
                    return false;

                await _client.DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }
            
            await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task DeleteAllAsync(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderName;
        request.Delimiter = null;

        do
        {
            var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (serviceObjects.Items != null)
            {
                foreach (var item in serviceObjects.Items)
                {
                    if (item == null)
                        continue;

                    await _client.DeleteObjectAsync(bucketName, item.Name, 
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            request.PageToken = serviceObjects.NextPageToken;
        }
        while (request.PageToken != null);
    }
    #endregion
}