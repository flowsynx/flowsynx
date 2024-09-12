using System.Net;
using Amazon.S3;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.Amazon.S3;

public class AmazonS3Storage : IPlugin
{
    private readonly ILogger<AmazonS3Storage> _logger;
    private readonly IStorageFilter _storageFilter;
    private AmazonS3StorageSpecifications? _s3StorageSpecifications;
    private AmazonS3Client _client = null!;
    private TransferUtility _fileTransferUtility = null!;

    public AmazonS3Storage(ILogger<AmazonS3Storage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public string Name => "Amazon.S3";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(AmazonS3StorageSpecifications);

    public Task Initialize()
    {
        _s3StorageSpecifications = Specifications.ToObject<AmazonS3StorageSpecifications>();
        _client = CreateClient(_s3StorageSpecifications);
        _fileTransferUtility = CreateTransferUtility(_client);
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

        var pathParts = GetPartsAsync(path);
        var isExist = await BucketExists(pathParts.BucketName, cancellationToken);
        if (!isExist)
        {
            await _client.PutBucketAsync(pathParts.BucketName, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            var isFolderExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
            if (!isFolderExist)
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

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException("Entered data is not valid. The data should be in string or Base64 format.");

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && writeFilters.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _fileTransferUtility.UploadAsync(dataStream, pathParts.BucketName,
                pathParts.RelativePath, cancellationToken).ConfigureAwait(false);

            var result = new StorageEntity(path, StorageEntityItemKind.File);
            return new { result.Id };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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

            var request = new GetObjectRequest { BucketName = pathParts.BucketName, Key = pathParts.RelativePath };
            var response = await _client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
            var fileExtension = Path.GetExtension(path);

            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);
            
            return new StorageRead()
            {
                Stream = new StorageStream(ms),
                ContentType = response.Headers.ContentType,
                Extension = fileExtension,
                Md5 = response.ETag.Trim('\"'),
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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
            var folder = pathParts.RelativePath;
            if (!folder.EndsWith(PathHelper.PathSeparator))
                folder += PathHelper.PathSeparator;

            await _client.DeleteObjectAsync(pathParts.BucketName, folder, cancellationToken);
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
            return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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

    public async Task<IEnumerable<TransmissionData>> PrepareTransmissionData(string entity, PluginFilters? filters,
            CancellationToken cancellationToken = new CancellationToken())
    {
        if (PathHelper.IsFile(entity))
        {
            var copyFile = await PrepareCopyFile(entity, cancellationToken);
            return new List<TransmissionData>() { copyFile };
        }

        return await PrepareCopyDirectory(entity, filters, cancellationToken);
    }

    private async Task<TransmissionData> PrepareCopyFile(string entity, CancellationToken cancellationToken = default)
    {
        var sourceStream = await ReadAsync(entity, null, cancellationToken);

        if (sourceStream is not StorageRead storageRead)
            throw new StorageException($"Copy operation for file '{entity} could not proceed!'");

        return new TransmissionData(entity, storageRead.Stream, storageRead.ContentType);
    }

    private async Task<IEnumerable<TransmissionData>> PrepareCopyDirectory(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(entity, filters, cancellationToken).ConfigureAwait(false);
        var storageEntities = entities.ToList().ConvertAll(item => (StorageList)item);

        var result = new List<TransmissionData>(storageEntities.Count);

        foreach (var entityItem in storageEntities)
        {
            TransmissionData transmissionData;
            if (string.Equals(entityItem.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                var read = await ReadAsync(entityItem.Path, null, cancellationToken);
                if (read is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entityItem.Name}' could be not read.");
                    continue;
                }
                transmissionData = new TransmissionData(entityItem.Path, storageRead.Stream, storageRead.ContentType);
            }
            else
            {
                transmissionData = new TransmissionData(entityItem.Path);
            }

            result.Add(transmissionData);
        }

        return result;
    }

    public async Task<IEnumerable<object>> TransmitDataAsync(string entity, PluginFilters? filters, IEnumerable<TransmissionData> transmissionData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = new List<object>();
        var data = transmissionData.ToList();
        foreach (var item in data)
        {
            switch (item.Content)
            {
                case null:
                    result.Add(await CreateAsync(item.Key, filters, cancellationToken));
                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    break;
                case StorageStream stream:
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateAsync(parentPath, filters, cancellationToken);
                        result.Add(await WriteAsync(item.Key, filters, stream, cancellationToken));
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                    break;
            }
        }

        return result;
    }
    
    public async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginFilters? filters,
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

            try
            {
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
                    Stream = storageRead.Stream,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                continue;
            }
        }

        return compressEntries;
    }

    #region private methods
    private AmazonS3Client CreateClient(AmazonS3StorageSpecifications specifications)
    {
        if (specifications.AccessKey == null)
            throw new ArgumentNullException(nameof(specifications.AccessKey));

        if (specifications.SecretKey == null)
            throw new ArgumentNullException(nameof(specifications.SecretKey));

        var awsCredentials = (string.IsNullOrEmpty(specifications.SessionToken))
            ? (AWSCredentials)new BasicAWSCredentials(specifications.AccessKey, specifications.SecretKey)
            : new SessionAWSCredentials(specifications.AccessKey, specifications.SecretKey, specifications.SessionToken);

        var config = CreateConfig(specifications.Region);
        return new AmazonS3Client(awsCredentials, config);
    }

    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }

    private AmazonS3StorageBucketPathPart GetPartsAsync(string fullPath)
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

        return new AmazonS3StorageBucketPathPart(bucketName, relativePath);
    }

    private async Task ListAsync(string bucketName, List<StorageEntity> result, string path,
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        using var browser = new AmazonS3BucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListAsync(path, listFilters, cancellationToken).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        var buckets = await _client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return buckets.Buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket.BucketName))
            .Select(bucket => bucket.BucketName).ToList();
    }

    private async Task<bool> ObjectExists(string bucketName, string key, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ListObjectsRequest { BucketName = bucketName, Prefix = key };
            var response = await _client.ListObjectsAsync(request, cancellationToken).ConfigureAwait(false);
            return response is { S3Objects.Count: > 0 };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var bucketsResponse = await _client.ListBucketsAsync(cancellationToken);
            return (bucketsResponse.Buckets.Any(x => x.BucketName == bucketName));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var request = new PutObjectRequest()
        {
            BucketName = bucketName,
            StorageClass = S3StorageClass.Standard,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
            Key = folderName,
            ContentBody = string.Empty
        };
        await _client.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    }

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (!isExist)
            {
                _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                return false;
            }

            await _client.DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private AmazonS3Config CreateConfig(string? region)
    {
        if (region == null)
            return new AmazonS3Config();

        return new AmazonS3Config
        {
            RegionEndpoint = ToRegionEndpoint(region),
            //ServiceURL = endpoint
        };
    }

    private RegionEndpoint ToRegionEndpoint(string? region)
    {
        if (region is null)
            throw new ArgumentNullException(nameof(region));

        return RegionEndpoint.GetBySystemName(region);
    }

    #endregion
    public void Dispose()
    {
        //throw new NotImplementedException();
    }
}