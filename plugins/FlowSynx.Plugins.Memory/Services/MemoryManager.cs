﻿using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Memory.Extensions;
using FlowSynx.Connectors.Storage.Memory.Models;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using System.Data;
using FlowSynx.Data;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;
using FlowSynx.Security;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public class MemoryManager: IMemoryManager
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly IMemoryMetrics _memoryMetrics;
    private readonly Dictionary<string, Dictionary<string, MemoryEntity>> _entities;

    public MemoryManager(ILogger logger, IDataService dataService, IDeserializer deserializer, IMemoryMetrics memoryMetrics)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        EnsureArg.IsNotNull(memoryMetrics, nameof(memoryMetrics));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _entities = new Dictionary<string, Dictionary<string, MemoryEntity>>();
        _memoryMetrics = memoryMetrics;
    }

    public Task<object> About(Context context)
    {
        long totalSpace, usedSpace, freeSpace;
        try
        {
            var metrics = _memoryMetrics.GetMetrics();
            totalSpace = metrics.Total;
            usedSpace = metrics.Used;
            freeSpace = metrics.Free;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            usedSpace = 0;
            freeSpace = 0;
        }

        return Task.FromResult<object>(new
        {
            Total = totalSpace,
            Free = freeSpace,
            Used = usedSpace
        });
    }

    public async Task Create(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        await CreateEntity(pathOptions.Path, createOptions).ConfigureAwait(false);
    }

    public async Task Write(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        await WriteEntity(pathOptions.Path, writeOptions).ConfigureAwait(false);
    }

    public async Task<InterchangeData> Read(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        return await ReadEntity(pathOptions.Path, readOptions).ConfigureAwait(false);
    }

    public Task Update(Context context)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        listOptions.Fields = null;

        var filteredEntities = await FilteredEntitiesList(path, listOptions).ConfigureAwait(false);

        var entityItems = filteredEntities.Rows;
        if (entityItems.Count <= 0)
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (DataRow entityItem in entityItems)
            await DeleteEntity(entityItem["FullPath"].ToString());

        if (deleteOptions.Purge is true)
            await PurgeEntity(path);
    }

    public async Task<bool> Exist(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();

        return await ExistEntity(pathOptions.Path).ConfigureAwait(false);
    }

    public async Task<InterchangeData> FilteredEntities(Context context)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var result = await FilteredEntitiesList(pathOptions.Path, listOptions).ConfigureAwait(false);
        return result;
    }

    public Task Transfer(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken)
    //{
    //    if (destinationContext.ConnectorContext?.Current is null)
    //        throw new StorageException(Resources.CalleeConnectorNotSupported);

    //    var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
    //    var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
    //    var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

    //    var transferData = await PrepareDataForTransferring(@namespace, type, sourcePathOptions.Path,
    //        sourceListOptions, sourceReadOptions);

    //    var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

    //    foreach (var row in transferData.Rows)
    //        row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

    //    await destinationContext.ConnectorContext.Current.ProcessTransfer(destinationContext, transferData, transferKind, cancellationToken);
    //}

    //public async Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken)
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
    //            var newWriteOption = new WriteOptions
    //            {
    //                Data = transferData.Content,
    //                Overwrite = writeOptions.Overwrite
    //            };

    //            await CreateEntity(parentPath, createOptions).ConfigureAwait(false);
    //            await WriteEntity(path, newWriteOption).ConfigureAwait(false);
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
    //                    await CreateEntity(item.Key, createOptions).ConfigureAwait(false);
    //                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    //                }
    //            }
    //            else
    //            {
    //                var parentPath = PathHelper.GetParent(item.Key);
    //                if (!PathHelper.IsRootPath(parentPath))
    //                {
    //                    var newWriteOption = new WriteOptions
    //                    {
    //                        Data = item.Content,
    //                        Overwrite = writeOptions.Overwrite,
    //                    };

    //                    await CreateEntity(parentPath, createOptions).ConfigureAwait(false);
    //                    await WriteEntity(item.Key, newWriteOption).ConfigureAwait(false);
    //                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    //                }
    //            }
    //        }
    //    }
    //}

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        var storageEntities = await EntitiesList(path, listOptions);

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
                var content = await ReadEntity(entityItem.FullPath, readOptions).ConfigureAwait(false);
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

    #region internal methods
    private async Task CreateEntity(string path, CreateOptions options)
    {
        path = PathHelper.ToUnixPath(path);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = GetParts(path);
        var isBucketExist = BucketExists(pathParts.BucketName);
        if (!isBucketExist)
        {
            _entities.Add(pathParts.BucketName, new Dictionary<string, MemoryEntity>());
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
            if (!isExist)
            {
                await AddFolder(pathParts.BucketName, pathParts.RelativePath).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation($"Directory '{pathParts.RelativePath}' is already exist.");
            }
        }
    }

    private Task WriteEntity(string path, WriteOptions options)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = options.Data.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        var pathParts = GetParts(path);
        var isBucketExist = BucketExists(pathParts.BucketName);
        if (!isBucketExist)
        {
            _entities.Add(pathParts.BucketName, new Dictionary<string, MemoryEntity>());
        }

        var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
        if (isExist && options.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var name = Path.GetFileName(path);
        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = new MemoryEntity(name, dataStream);
        bucket[pathParts.RelativePath] = memoryEntity;

        return Task.CompletedTask;
    }

    private Task<InterchangeData> ReadEntity(string path, ReadOptions options)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var pathParts = GetParts(path);
        var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);

        if (!isExist)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = bucket[pathParts.RelativePath];

        var result = new InterchangeData();
        result.Columns.Add("Content", typeof(byte[]));

        var row = result.NewRow();
        row["Content"] = memoryEntity.Content ?? Array.Empty<byte>();

        return Task.FromResult(result);
    }

    private Task DeleteEntity(string? path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetParts(path);
        var bucket = _entities[pathParts.BucketName];

        if (PathHelper.IsFile(path))
        {
            var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
            if (!isExist)
            {
                _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                return Task.CompletedTask;
            }

            bucket.Remove(pathParts.RelativePath);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
            return Task.CompletedTask;
        }

        var folderPrefix = !pathParts.RelativePath.EndsWith(PathHelper.PathSeparator)
            ? pathParts.RelativePath + PathHelper.PathSeparator
            : pathParts.RelativePath;

        var folderExist = bucket.Keys.Any(x => x.StartsWith(folderPrefix));

        if (!folderExist)
        {
            _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
            return Task.CompletedTask;
        }

        var itemToDelete = bucket.Keys.Where(x => x.StartsWith(folderPrefix)).Select(p => p);
        foreach (var item in itemToDelete)
        {
            bucket.Remove(item);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, item));
        }

        return Task.CompletedTask;
    }

    private Task PurgeEntity(string? path)
    {
        path = PathHelper.ToUnixPath(path);
        var pathParts = GetParts(path);
        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            var bucket = _entities[pathParts.BucketName];
            var itemToDelete = bucket.Keys.Where(x => x.StartsWith(pathParts.RelativePath)).Select(p => p);

            var toDelete = itemToDelete.ToList();
            if (toDelete.Any())
            {
                foreach (var item in toDelete)
                {
                    bucket.Remove(item);
                }
            }
        }
        else
        {
            if (string.IsNullOrEmpty(pathParts.RelativePath) || PathHelper.IsRootPath(pathParts.RelativePath))
                _entities.Remove(pathParts.BucketName);
        }

        return Task.CompletedTask;
    }

    private Task<bool> ExistEntity(string path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetParts(path);
        if (PathHelper.IsFile(path))
        {
            return Task.FromResult(ObjectExists(pathParts.BucketName, pathParts.RelativePath));
        }

        var folderExist = FolderExist(pathParts.BucketName, pathParts.RelativePath);
        return Task.FromResult(folderExist);
    }

    private async Task<InterchangeData> FilteredEntitiesList(string path, ListOptions listOptions)
    {
        path = PathHelper.ToUnixPath(path);
        var entities = await EntitiesList(path, listOptions);

        var dataFilterOptions = GetDataTableOption(listOptions);
        var dataTable = entities.ListToInterchangeData();
        var filteredEntities = _dataService.Select(dataTable, dataFilterOptions);

        return filteredEntities;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesList(string path, ListOptions listOptions)
    {
        path = PathHelper.ToUnixPath(path);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

        var pathParts = GetParts(path);

        if (pathParts.BucketName == "")
        {
            if (pathParts.RelativePath != "")
                throw new StorageException(Resources.BucketNameIsRequired);

            buckets.AddRange(await ListBuckets().ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listOptions.IncludeMetadata)));
            return storageEntities;
        }

        buckets.Add(pathParts.BucketName);

        await Task.WhenAll(buckets.Select(b =>
            List(storageEntities, b, pathParts.RelativePath, listOptions))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path,
    //    ListOptions listOptions, ReadOptions readOptions)
    //{
    //    path = PathHelper.ToUnixPath(path);

    //    var storageEntities = await EntitiesList(path, listOptions);

    //    var fields = GetFields(listOptions.Fields);
    //    var kindFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("Kind", StringComparison.OrdinalIgnoreCase));
    //    var fullPathFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

    //    if (!kindFieldExist)
    //        fields.Append("Kind");

    //    if (!fullPathFieldExist)
    //        fields.Append("FullPath");

    //    var dataFilterOptions = GetDataTableOption(listOptions);

    //    var dataTable = storageEntities.ListToDataTable();
    //    var filteredData = _dataService.Select(dataTable, dataFilterOptions);
    //    var transferDataRows = new List<TransferDataRow>();

    //    foreach (DataRow row in filteredData.Rows)
    //    {
    //        var content = string.Empty;
    //        var contentType = string.Empty;
    //        var fullPath = row["FullPath"].ToString() ?? string.Empty;

    //        if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
    //        {
    //            if (!string.IsNullOrEmpty(fullPath))
    //            {
    //                var read = await ReadEntity(fullPath, readOptions).ConfigureAwait(false);
    //                content = read.Content.ToBase64String();
    //            }
    //        }

    //        if (!kindFieldExist)
    //            row["Kind"] = DBNull.Value;

    //        if (!fullPathFieldExist)
    //            row["FullPath"] = DBNull.Value;

    //        var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
    //        transferDataRows.Add(new TransferDataRow
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
    //        Rows = transferDataRows
    //    };

    //    return result;
    //}

    private MemoryStoragePathPart GetParts(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return new MemoryStoragePathPart();

        string bucketName, relativePath;
        fullPath = PathHelper.Normalize(fullPath);
        var symbolIndex = fullPath.IndexOf('/');

        if (symbolIndex < 0)
        {
            bucketName = fullPath;
            relativePath = string.Empty;
        }
        else
        {
            bucketName = fullPath[..(symbolIndex)];
            relativePath = fullPath[(symbolIndex + 1)..];
        }

        return new MemoryStoragePathPart(bucketName, relativePath);
    }

    private bool FolderExist(string bucketName, string path)
    {
        if (!BucketExists(bucketName))
            return false;

        var bucket = _entities[bucketName];
        var folderPrefix = path + PathHelper.PathSeparator;
        return bucket.Keys.Any(x => x.StartsWith(folderPrefix));
    }

    private bool BucketExists(string bucketName)
    {
        try
        {
            return _entities.ContainsKey(bucketName);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ObjectExists(string bucketName, string path)
    {
        try
        {
            if (!BucketExists(bucketName))
                return false;

            var bucket = _entities[bucketName];
            return bucket.ContainsKey(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private Task<string> AddFolder(string bucketName, string folderName)
    {
        var memoryEntity = new MemoryEntity(folderName);
        var bucket = _entities[bucketName];

        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        bucket[folderName] = memoryEntity;
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
        return Task.FromResult(memoryEntity.Id);
    }

    private Task<List<string>> ListBuckets()
    {
        var buckets = _entities.Keys;
        var result = buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket))
            .Select(bucket => bucket).ToList();

        return Task.FromResult(result);
    }

    private Task List(ICollection<StorageEntity> result, string bucketName, string path,
        ListOptions listOptions)
    {
        if (!_entities.ContainsKey(bucketName))
            throw new Exception(string.Format(Resources.BucketNotExist, bucketName));

        var bucket = _entities[bucketName];

        foreach (var (key, _) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memEntity = bucket[key];
                result.Add(memEntity.ToEntity(bucketName, listOptions.IncludeMetadata));
            }
        }

        return Task.CompletedTask;
    }

    private SelectDataOption GetDataTableOption(ListOptions options) => new()
    {
        Fields = GetFields(options.Fields),
        Filter = GetFilterList(options.Filter),
        Sort = GetSortList(options.Sort),
        CaseSensitive = options.CaseSensitive,
        Paging = GetPaging(options.Paging),
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

    private Paging GetPaging(string? json)
    {
        var result = new Paging();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<Paging>(json);
        }

        return result;
    }

    //private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    //{
    //    return dataTable.Columns.Cast<DataColumn>()
    //        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    //}
    #endregion
}