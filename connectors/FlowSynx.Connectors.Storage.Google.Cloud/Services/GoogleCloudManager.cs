using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Google.Cloud.Extensions;
using FlowSynx.Connectors.Storage.Google.Cloud.Models;
using FlowSynx.Connectors.Storage.Exceptions;
using Google;
using System.Net;
using System.Text;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Storage.Google.Cloud.Services;

internal class GoogleCloudManager : IGoogleCloudManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly StorageClient _client;
    private readonly GoogleCloudSpecifications? _specifications;

    public GoogleCloudManager(ILogger logger, StorageClient client, GoogleCloudSpecifications? specifications)
    {
        _logger = logger;
        _client = client;
        _specifications = specifications;
    }

    public async Task CreateAsync(string entity, CreateOptions options, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        if (_specifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var pathParts = GetPartsAsync(path);
        var isExist = await BucketExists(pathParts.BucketName, cancellationToken);
        if (!isExist)
        {
            await _client.CreateBucketAsync(_specifications.ProjectId, pathParts.BucketName,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
            await AddFolder(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(string entity, WriteOptions options, object dataOptions, 
        CancellationToken cancellationToken)
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

            await _client.UploadObjectAsync(pathParts.BucketName, pathParts.RelativePath,
                null, dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<ReadResult> ReadAsync(string entity, ReadOptions options, CancellationToken cancellationToken)
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

            var ms = new MemoryStream();
            var response = await _client.DownloadObjectAsync(pathParts.BucketName,
                pathParts.RelativePath, ms, cancellationToken: cancellationToken).ConfigureAwait(false);

            ms.Position = 0;

            return new ReadResult
            {
                Content = ms.ToArray(),
                ContentHash = Convert.FromBase64String(response.Md5Hash).ToHexString(),
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task DeleteAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            if (PathHelper.IsFile(path))
            {
                var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
                if (!isExist)
                {
                    _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                    return;
                }

                await _client.DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task PurgeAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
        await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistAsync(string entity, CancellationToken cancellationToken)
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

    public async Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions listOptions,
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
            buckets.AddRange(await ListBucketsAsync().ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listOptions.IncludeMetadata)));

            if (!listOptions.Recurse)
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
            ListObjectsAsync(b, storageEntities, path, listOptions, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private async Task ListObjectsAsync(string bucketName, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var objects = await 
            ListFolderAsync(bucketName, path, listOptions, cancellationToken)
            .ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<StorageEntity>> ListFolderAsync(string bucketName, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();

        try
        {
            var request = _client.Service.Objects.List(bucketName);
            request.Prefix = FormatFolderPrefix(path);
            request.Delimiter = listOptions.Recurse ? null : PathHelper.PathSeparatorString;

            do
            {
                var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                if (serviceObjects.Items != null)
                {
                    foreach (var item in serviceObjects.Items)
                    {
                        if (item == null)
                            continue;

                        result.Add(item.Name.EndsWith(PathHelper.PathSeparator)
                            ? item.ToEntity(true, listOptions.IncludeMetadata)
                            : item.ToEntity(false, listOptions.IncludeMetadata));
                    }
                }

                if (serviceObjects.Prefixes != null)
                    result.AddRange(serviceObjects.Prefixes.Select(p => new StorageEntity(p, StorageEntityItemKind.Directory)));

                request.PageToken = serviceObjects.NextPageToken;
            }
            while (request.PageToken != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return result;
    }

    public void Dispose()
    {
    }

    #region internal methods
    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var content = Encoding.UTF8.GetBytes("");
        await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory",
            new MemoryStream(content), cancellationToken: cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
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

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync()
    {
        if (_specifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var result = new List<string>();

        await foreach (var bucket in _client.ListBucketsAsync(_specifications.ProjectId).ConfigureAwait(false))
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

    private GoogleCloudPathPart GetPartsAsync(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        if (fullPath == null)
            throw new ArgumentNullException(nameof(fullPath));

        string bucketName, relativePath;
        var parts = PathHelper.Split(fullPath);

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

        return new GoogleCloudPathPart(bucketName, relativePath);
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
    #endregion
}
