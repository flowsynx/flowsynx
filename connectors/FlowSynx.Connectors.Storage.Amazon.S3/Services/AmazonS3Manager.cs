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
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.IO.Serialization;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public class AmazonS3Manager : IAmazonS3Manager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly AmazonS3Client _client;
    private readonly TransferUtility _fileTransferUtility;

    public AmazonS3Manager(ILogger logger, AmazonS3Client client, IDataFilter dataFilter, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _client = client;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _fileTransferUtility = CreateTransferUtility(_client);
    }

    public Task<object> About(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public async Task CreateAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        await CreateEntityAsync(pathOptions.Path, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        await WriteEntityAsync(pathOptions.Path, writeOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        return await ReadEntityAsync(pathOptions.Path, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteFilters = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        listOptions.Fields = null;

        var filteredEntities = await FilteredEntitiesListAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

        var entityItems = filteredEntities.Rows;
        if (entityItems.Count <= 0)
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (DataRow entityItem in entityItems)
            await DeleteObject(entityItem["FullPath"].ToString(), cancellationToken).ConfigureAwait(false);

        if (deleteFilters.Purge is true)
            await PurgeAsync(path, cancellationToken);
    }

    public async Task<bool> ExistAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
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

    public async Task<IEnumerable<object>> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var filteredEntities = await FilteredEntitiesListAsync(pathOptions.Path, listOptions, cancellationToken);

        return filteredEntities.CreateListFromTable();
    }

    public async Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
        var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
        var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

        var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);

        var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

        foreach (var row in transferData.Rows)
            row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);

        if (!string.IsNullOrEmpty(transferData.Content))
        {
            var parentPath = PathHelper.GetParent(path);
            if (!PathHelper.IsRootPath(parentPath))
            {
                await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);

                var clonedWriteOptions = (WriteOptions)writeOptions.Clone();
                clonedWriteOptions.Data = transferData.Content;
                await WriteEntityAsync(path, clonedWriteOptions, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation($"Copy operation done for entity '{path}'");
            }
        }
        else
        {
            foreach (var item in transferData.Rows)
            {
                if (string.IsNullOrEmpty(item.Content))
                {
                    if (transferData.Namespace == Namespace.Storage)
                    {
                        await CreateEntityAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);

                        var clonedWriteOptions = (WriteOptions)writeOptions.Clone();
                        clonedWriteOptions.Data = item.Content;
                        await WriteEntityAsync(item.Key, clonedWriteOptions, cancellationToken).ConfigureAwait(false);

                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
            }
        }
    }

    public async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        var listOptions = context.Options.ToObject<ListOptions>();

        var storageEntities = await EntitiesListAsync(path, listOptions, cancellationToken);

        var entityItems = storageEntities.ToList();
        if (!entityItems.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in entityItems)
        {
            if (!string.Equals(entityItem.Kind, StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entityItem.Name}' is not a file.");
                continue;
            }

            try
            {
                var readOptions = new ReadOptions { Hashing = false };
                var content = await ReadEntityAsync(entityItem.FullPath, readOptions, cancellationToken).ConfigureAwait(false);
                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
                    Content = content.Content,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return compressEntries;
    }

    public void Dispose() { }

    #region internal methods
    private async Task CreateEntityAsync(string path, CreateOptions createOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    private async Task WriteEntityAsync(string path, WriteOptions writeOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = writeOptions.Data.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && writeOptions.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _fileTransferUtility.UploadAsync(dataStream, pathParts.BucketName,
                pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<ReadResult> ReadEntityAsync(string path, ReadOptions readOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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
                ContentHash = readOptions.Hashing is true ? response.ETag.Trim('\"') : string.Empty,
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
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
            return bucketsResponse.Buckets.Any(x => x.BucketName == bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task DeleteObject(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    private async Task PurgeAsync(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var folder = path;
        if (!folder.EndsWith(PathHelper.PathSeparator))
            folder += PathHelper.PathSeparator;

        await DeleteObject(folder, cancellationToken);
    }

    private async Task<DataTable> FilteredEntitiesListAsync(string path, ListOptions listOptions, 
        CancellationToken cancellationToken)
    {
        var entities = await EntitiesListAsync(path, listOptions, cancellationToken);
        var dataFilterOptions = GetFilterOptions(listOptions);
        var dataTable = entities.ToDataTable();
        var filteredEntities = _dataFilter.Filter(dataTable, dataFilterOptions);

        return filteredEntities;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesListAsync(string path, ListOptions listOptions, 
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listOptions.IncludeMetadata)));

            if (!listOptions.Recurse)
                return storageEntities;
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

    private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type,
        Context context, CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var entities = await EntitiesListAsync(pathOptions.Path, listOptions, cancellationToken);

        var fields = GetFields(listOptions.Fields);
        var kindFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("Kind", StringComparison.OrdinalIgnoreCase));
        var fullPathFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

        if (!kindFieldExist)
            fields = fields.Append("Kind").ToArray();

        if (!fullPathFieldExist)
            fields = fields.Append("FullPath").ToArray();

        var dataFilterOptions = GetFilterOptions(listOptions);

        var dataTable = entities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var transferDataRow = new List<TransferDataRow>();

        foreach (DataRow row in filteredData.Rows)
        {
            var content = string.Empty;
            var contentType = string.Empty;
            var fullPath = row["FullPath"].ToString() ?? string.Empty;

            if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var readOptions = new ReadOptions { Hashing = false };
                    var read = await ReadEntityAsync(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
                    content = read.Content.ToBase64String();
                }
            }

            if (!kindFieldExist)
                row["Kind"] = DBNull.Value;

            if (!fullPathFieldExist)
                row["FullPath"] = DBNull.Value;

            var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
            transferDataRow.Add(new TransferDataRow
            {
                Key = fullPath,
                ContentType = contentType,
                Content = content,
                Items = itemArray
            });
        }

        if (!kindFieldExist)
            filteredData.Columns.Remove("Kind");

        if (!fullPathFieldExist)
            filteredData.Columns.Remove("FullPath");

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        var result = new TransferData
        {
            Namespace = @namespace,
            ConnectorType = type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRow
        };

        return result;
    }

    private DataFilterOptions GetFilterOptions(ListOptions options)
    {
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = GetFields(options.Fields),
            FilterExpression = options.Filter,
            Sort = GetSorts(options.Sort),
            CaseSensitive = options.CaseSensitive,
            Limit = options.Limit,
        };

        return dataFilterOptions;
    }

    private string[] GetFields(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private Sort[] GetSorts(string? sorts)
    {
        var result = Array.Empty<Sort>();
        if (!string.IsNullOrEmpty(sorts))
        {
            result = _deserializer.Deserialize<Sort[]>(sorts);
        }

        return result;
    }

    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }
    #endregion
}