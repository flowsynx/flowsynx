using Amazon.S3;
using Amazon.S3.Model;
using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Exceptions;
using System.Net;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using Amazon.S3.Transfer;
using EnsureThat;
using FlowSynx.Connectors.Storage.Amazon.S3.Models;
using FlowSynx.Connectors.Storage.Amazon.S3.Extensions;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public class AmazonS3Browser : IAmazonS3Browser, IDisposable
{
    private readonly ILogger _logger;
    private readonly AmazonS3Client _client;
    private TransferUtility _fileTransferUtility = null!;

    public AmazonS3Browser(ILogger logger, AmazonS3Client client)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        _logger = logger;
        _client = client;
        _fileTransferUtility = CreateTransferUtility(_client);
    }

    public async Task CreateAsync(string entity, CreateOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
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
    }

    public async Task WriteAsync(string entity, WriteOptions options,
        object dataOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _fileTransferUtility.UploadAsync(dataStream, pathParts.BucketName,
                pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<ReadResult> ReadAsync(string entity, ReadOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
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

            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);

            return new ReadResult
            {
                Content = ms.ToArray(),
                ContentHash = options.Hashing is true ? response.ETag.Trim('\"') : string.Empty,
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
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
                return;
            }

            await _client
                .DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<bool> ExistAsync(string entity, CancellationToken cancellationToken)
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

    public async Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(options.IncludeMetadata)));

            if (!options.Recurse)
            {
                return storageEntities;
            }
        }
        else
        {
            var pathParts = GetPartsAsync(path);
            path = pathParts.RelativePath;
            buckets.Add(pathParts.BucketName);
        }

        await Task.WhenAll(buckets.Select(b =>
            ListObjectsAsync(b, storageEntities, path, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    public void Dispose() { }

    #region internal methods
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
            return bucketsResponse.Buckets.Any(x => x.BucketName == bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        var buckets = await _client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return buckets.Buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket.BucketName))
            .Select(bucket => bucket.BucketName).ToList();
    }

    private async Task ListObjectsAsync(string bucketName, List<StorageEntity> result, string path,
    ListOptions listOptions, CancellationToken cancellationToken)
    {
        var entities = new List<StorageEntity>();
        await ListFolderAsync(entities, bucketName, path, listOptions, cancellationToken).ConfigureAwait(false);

        if (entities.Count > 0)
        {
            result.AddRange(entities);
        }
    }

    private async Task ListFolderAsync(List<StorageEntity> entities, string bucketName, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request()
        {
            BucketName = bucketName,
            Prefix = FormatFolderPrefix(path),
            Delimiter = PathHelper.PathSeparatorString
        };

        var result = new List<StorageEntity>();
        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
            result.AddRange(response.ToEntity(_client, bucketName, listOptions.IncludeMetadata, cancellationToken));

            if (response.NextContinuationToken == null)
                break;

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);

        entities.AddRange(result);

        if (listOptions.Recurse)
        {
            var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(directories.Select(f => ListFolderAsync(entities, bucketName, GetRelativePath(f.FullPath),
                listOptions, cancellationToken))).ConfigureAwait(false);
        }
    }

    private string? FormatFolderPrefix(string folderPath)
    {
        folderPath = PathHelper.Normalize(folderPath);

        if (PathHelper.IsRootPath(folderPath))
            return null;

        if (!folderPath.EndsWith(PathHelper.PathSeparator))
            folderPath += PathHelper.PathSeparator;

        return folderPath;
    }

    private string GetRelativePath(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        string[] parts = PathHelper.Split(fullPath);
        return parts.Length == 1 ? string.Empty : PathHelper.Combine(parts.Skip(1));
    }

    private AmazonS3EntityPart GetPartsAsync(string fullPath)
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

        return new AmazonS3EntityPart(bucketName, relativePath);
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

    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }
    #endregion
}