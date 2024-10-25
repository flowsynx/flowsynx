using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Memory.Extensions;
using FlowSynx.Connectors.Storage.Memory.Models;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.Data.Extensions;
using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public class MemoryManager: IMemoryManager
{
    private readonly ILogger _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly IMemoryMetrics _memoryMetrics;
    private readonly Dictionary<string, Dictionary<string, MemoryEntity>> _entities;

    public MemoryManager(ILogger logger, IDataFilter dataFilter, IDeserializer deserializer, IMemoryMetrics memoryMetrics)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        EnsureArg.IsNotNull(memoryMetrics, nameof(memoryMetrics));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _entities = new Dictionary<string, Dictionary<string, MemoryEntity>>();
        _memoryMetrics = memoryMetrics;
    }

    public Task<object> GetStatisticsAsync()
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

    public async Task CreateAsync(string entity, CreateOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = GetPartsAsync(path);
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

    public Task WriteAsync(string entity, WriteOptions options, object dataOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        var pathParts = GetPartsAsync(path);
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

    public Task<ReadResult> ReadAsync(string entity, ReadOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var pathParts = GetPartsAsync(path);
        var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);

        if (!isExist)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = bucket[pathParts.RelativePath];

        var result = new ReadResult
        {
            Content = memoryEntity.Content ?? Array.Empty<byte>()
        };

        return Task.FromResult(result);
    }

    public Task DeleteAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
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

    public Task PurgeAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        var pathParts = GetPartsAsync(path);
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

    public Task<bool> ExistAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
        if (PathHelper.IsFile(path))
        {
            return Task.FromResult(ObjectExists(pathParts.BucketName, pathParts.RelativePath));
        }

        var folderExist = FolderExistAsync(pathParts.BucketName, pathParts.RelativePath);
        return Task.FromResult(folderExist);
    }

    public async Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions listOptions)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

        var pathParts = GetPartsAsync(path);

        if (pathParts.BucketName == "")
        {
            if (pathParts.RelativePath != "")
                throw new StorageException(Resources.BucketNameIsRequired);

            buckets.AddRange(await ListBucketsAsync().ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(listOptions.IncludeMetadata)));
            return storageEntities;
        }

        buckets.Add(pathParts.BucketName);

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(storageEntities, b, pathParts.RelativePath, listOptions))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    public async Task<IEnumerable<object>> FilteredEntitiesAsync(string entity, ListOptions listOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await EntitiesAsync(path, listOptions);

        var dataFilterOptions = GetFilterOptions(listOptions);
        var dataTable = entities.ToDataTable();
        var filteredEntities = _dataFilter.Filter(dataTable, dataFilterOptions);

        return filteredEntities.CreateListFromTable();
    }

    public async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string entity,
        ListOptions listOptions, ReadOptions readOptions)
    {
        var path = PathHelper.ToUnixPath(entity);

        var storageEntities = await EntitiesAsync(path, listOptions);

        var fields = GetFields(listOptions.Fields);
        var kindFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("Kind", StringComparison.OrdinalIgnoreCase));
        var fullPathFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

        if (!kindFieldExist)
            fields = fields.Append("Kind").ToArray();

        if (!fullPathFieldExist)
            fields = fields.Append("FullPath").ToArray();

        var dataFilterOptions = GetFilterOptions(listOptions);

        var dataTable = storageEntities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var transferDataRows = new List<TransferDataRow>();

        foreach (DataRow row in filteredData.Rows)
        {
            var content = string.Empty;
            var contentType = string.Empty;
            var fullPath = row["FullPath"].ToString() ?? string.Empty;

            if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var read = await ReadAsync(fullPath, readOptions).ConfigureAwait(false);
                    content = read.Content.ToBase64String();
                }
            }

            if (!kindFieldExist)
                row["Kind"] = DBNull.Value;

            if (!fullPathFieldExist)
                row["FullPath"] = DBNull.Value;

            var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
            transferDataRows.Add(new TransferDataRow
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
            Rows = transferDataRows
        };

        return result;
    }

    #region internal methods
    private MemoryStoragePathPart GetPartsAsync(string fullPath)
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

    private bool FolderExistAsync(string bucketName, string path)
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

    private Task<List<string>> ListBucketsAsync()
    {
        var buckets = _entities.Keys;
        var result = buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket))
            .Select(bucket => bucket).ToList();

        return Task.FromResult(result);
    }

    private Task ListAsync(ICollection<StorageEntity> result, string bucketName, string path,
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


    private DataFilterOptions GetFilterOptions(ListOptions options)
    {
        var fields = GetFields(options.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = options.Filter,
            SortExpression = options.Sort,
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
    #endregion
}