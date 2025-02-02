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
using System.Data;
using FlowSynx.IO.Serialization;
using FlowSynx.IO.Compression;
using FlowSynx.Data;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public class AmazonS3Manager : IAmazonS3Manager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly AmazonS3Client _client;
    private readonly TransferUtility _fileTransferUtility;

    public AmazonS3Manager(ILogger logger, AmazonS3Client client, IDataService dataService, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _client = client;
        _dataService = dataService;
        _deserializer = deserializer;
        _fileTransferUtility = CreateTransferUtility(_client);
    }

    public async Task Create(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        await CreateEntity(pathOptions.Path, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task Write(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        await WriteEntity(pathOptions.Path, writeOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InterchangeData> Read(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        return await ReadEntity(pathOptions.Path, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public Task Update(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteFilters = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        listOptions.Fields = null;

        var filteredEntities = await FilteredEntitiesList(path, listOptions, cancellationToken).ConfigureAwait(false);

        var entityItems = filteredEntities.Rows;
        if (entityItems.Count <= 0)
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (DataRow entityItem in entityItems)
            await DeleteObject(entityItem["FullPath"].ToString(), cancellationToken).ConfigureAwait(false);

        if (deleteFilters.Purge is true)
            await Purge(path, cancellationToken);
    }

    public async Task<bool> Exist(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetParts(path);
            return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var filteredEntities = await FilteredEntitiesList(pathOptions.Path, listOptions, cancellationToken);
        return filteredEntities;
    }

    public Task Transfer(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default)
    //{
    //    var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
    //    var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
    //    var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

    //    var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);

    //    var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

    //    foreach (var row in transferData.Rows)
    //        row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

    //    await destinationContext.ConnectorContext.Current.ProcessTransfer(destinationContext, transferData, transferKind, cancellationToken);
    //}

    //public async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var createOptions = context.Options.ToObject<CreateOptions>();
    //    var writeOptions = context.Options.ToObject<WriteOptions>();

    //    var path = PathHelper.ToUnixPath(pathOptions.Path);

    //    if (!string.IsNullOrEmpty(transferData.Content))
    //    {
    //        var parentPath = PathHelper.GetParent(path);
    //        if (!PathHelper.IsRootPath(parentPath))
    //        {
    //            await CreateEntity(parentPath, createOptions, cancellationToken).ConfigureAwait(false);

    //            var clonedWriteOptions = (WriteOptions)writeOptions.Clone();
    //            clonedWriteOptions.Data = transferData.Content;
    //            await WriteEntity(path, clonedWriteOptions, cancellationToken).ConfigureAwait(false);

    //            _logger.LogInformation($"Copy operation done for entity '{path}'");
    //        }
    //    }
    //    else
    //    {
    //        foreach (var item in transferData.Rows)
    //        {
    //            if (string.IsNullOrEmpty(item.Content))
    //            {
    //                if (transferData.Namespace == Namespace.Storage)
    //                {
    //                    await CreateEntity(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
    //                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    //                }
    //            }
    //            else
    //            {
    //                var parentPath = PathHelper.GetParent(item.Key);
    //                if (!PathHelper.IsRootPath(parentPath))
    //                {
    //                    await CreateEntity(parentPath, createOptions, cancellationToken).ConfigureAwait(false);

    //                    var clonedWriteOptions = (WriteOptions)writeOptions.Clone();
    //                    clonedWriteOptions.Data = item.Content;
    //                    await WriteEntity(item.Key, clonedWriteOptions, cancellationToken).ConfigureAwait(false);

    //                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    //                }
    //            }
    //        }
    //    }
    //}

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        var listOptions = context.Options.ToObject<ListOptions>();

        var storageEntities = await EntitiesList(path, listOptions, cancellationToken);

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
                var content = await ReadEntity(entityItem.FullPath, readOptions, cancellationToken).ConfigureAwait(false);
                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
                    Content = (byte[])content.Rows[0]["Content"],
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
    private async Task CreateEntity(string path, CreateOptions createOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = GetParts(path);
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

    private async Task WriteEntity(string path, WriteOptions writeOptions, CancellationToken cancellationToken)
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
            var pathParts = GetParts(path);
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

    private async Task<InterchangeData> ReadEntity(string path, ReadOptions readOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetParts(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var request = new GetObjectRequest { BucketName = pathParts.BucketName, Key = pathParts.RelativePath };
            var response = await _client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);

            var result = new InterchangeData();
            result.Columns.Add("Content", typeof(byte[]));

            var row = result.NewRow();
            row.Metadata.ContentHash = readOptions.Hashing is true ? response.ETag.Trim('\"') : string.Empty;
            row["Content"] = ms.ToArray();

            return result;
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
            var pathParts = GetParts(path);
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

    private async Task Purge(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var folder = path;
        if (!folder.EndsWith(PathHelper.PathSeparator))
            folder += PathHelper.PathSeparator;

        await DeleteObject(folder, cancellationToken);
    }

    private async Task<InterchangeData> FilteredEntitiesList(string path, ListOptions listOptions, 
        CancellationToken cancellationToken)
    {
        var entities = await EntitiesList(path, listOptions, cancellationToken);
        var dataFilterOptions = GetDataTableOption(listOptions);
        var dataTable = entities.ListToInterchangeData();
        var filteredEntities = _dataService.Select(dataTable, dataFilterOptions);

        return (InterchangeData)filteredEntities;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesList(string path, ListOptions listOptions, 
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
            buckets.AddRange(await ListBuckets(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listOptions.IncludeMetadata)));

            if (!listOptions.Recurse)
                return storageEntities;
        }
        else
        {
            var pathParts = GetParts(path);
            path = pathParts.RelativePath;
            buckets.Add(pathParts.BucketName);
        }

        await Task.WhenAll(buckets.Select(b =>
            ListObjects(b, storageEntities, path, listOptions, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private async Task<IReadOnlyCollection<string>> ListBuckets(CancellationToken cancellationToken)
    {
        var buckets = await _client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return buckets.Buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket.BucketName))
            .Select(bucket => bucket.BucketName).ToList();
    }

    private async Task ListObjects(string bucketName, List<StorageEntity> result, string path,
    ListOptions listOptions, CancellationToken cancellationToken)
    {
        var entities = new List<StorageEntity>();
        await ListFolder(entities, bucketName, path, listOptions, cancellationToken).ConfigureAwait(false);

        if (entities.Count > 0)
        {
            result.AddRange(entities);
        }
    }

    private async Task ListFolder(List<StorageEntity> entities, string bucketName, string path,
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
            await Task.WhenAll(directories.Select(f => ListFolder(entities, bucketName, GetRelativePath(f.FullPath),
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

    private AmazonS3EntityPart GetParts(string fullPath)
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

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type,
    //    Context context, CancellationToken cancellationToken = default)
    //{
    //    if (context.ConnectorContext?.Current is null)
    //        throw new StorageException(Resources.CalleeConnectorNotSupported);

    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();

    //    var entities = await EntitiesList(pathOptions.Path, listOptions, cancellationToken);

    //    var fields = GetFields(listOptions.Fields);
    //    var kindFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("Kind", StringComparison.OrdinalIgnoreCase));
    //    var fullPathFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

    //    if (!kindFieldExist)
    //         fields.Append("Kind");

    //    if (!fullPathFieldExist)
    //        fields.Append("FullPath");

    //    var dataFilterOptions = GetDataTableOption(listOptions);

    //    var dataTable = entities.ListToDataTable();
    //    var filteredData = _dataService.Select(dataTable, dataFilterOptions);
    //    var transferDataRow = new List<TransferDataRow>();

    //    foreach (DataRow row in filteredData.Rows)
    //    {
    //        var content = string.Empty;
    //        var contentType = string.Empty;
    //        var fullPath = row["FullPath"].ToString() ?? string.Empty;

    //        if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
    //        {
    //            if (!string.IsNullOrEmpty(fullPath))
    //            {
    //                var readOptions = new ReadOptions { Hashing = false };
    //                var read = await ReadEntity(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
    //                content = read.Content.ToBase64String();
    //            }
    //        }

    //        if (!kindFieldExist)
    //            row["Kind"] = DBNull.Value;

    //        if (!fullPathFieldExist)
    //            row["FullPath"] = DBNull.Value;

    //        var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
    //        transferDataRow.Add(new TransferDataRow
    //        {
    //            Key = fullPath,
    //            ContentType = contentType,
    //            Content = content,
    //            Items = itemArray
    //        });
    //    }

    //    if (!kindFieldExist)
    //        filteredData.Columns.Remove("Kind");

    //    if (!fullPathFieldExist)
    //        filteredData.Columns.Remove("FullPath");

    //    var result = new TransferData
    //    {
    //        Namespace = @namespace,
    //        ConnectorType = type,
    //        Columns = GetTransferDataColumn(filteredData),
    //        Rows = transferDataRow
    //    };

    //    return result;
    //}

    private SelectDataOption GetDataTableOption(ListOptions options) => new()
    {
        Fields = GetFields(options.Fields),
        Filter = GetFilterList(options.Filter),
        Sort = GetSortList(options.Sort),
        CaseSensitive = options.CaseSensitive,
        Paging = GetFetch(options.Paging),
    };

    private FieldsList GetFields(string? json)
    {
        var result = new FieldsList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FieldsList>(json);
        }

        return result;
    }

    private FilterList GetFilterList(string? json)
    {
        var result = new FilterList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FilterList>(json);
        }

        return result;
    }

    private SortList GetSortList(string? json)
    {
        var result = new SortList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<SortList>(json);
        }

        return result;
    }

    private Paging GetFetch(string? json)
    {
        var result = new Paging();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<Paging>(json);
        }

        return result;
    }

    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }

    //private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    //{
    //    return dataTable.Columns.Cast<DataColumn>()
    //        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    //}
    #endregion
}