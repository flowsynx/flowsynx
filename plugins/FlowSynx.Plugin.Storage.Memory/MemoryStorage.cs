using EnsureThat;
using FlowSynx.Data.Extensions;
using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Options;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryStorage : PluginBase
{
    private readonly ILogger<MemoryStorage> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly Dictionary<string, Dictionary<string, MemoryEntity>> _entities;

    public MemoryStorage(ILogger<MemoryStorage> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _entities = new Dictionary<string, Dictionary<string, MemoryEntity>>();
    }

    public override Guid Id => Guid.Parse("ac220180-021e-4150-b0e1-c4d4bdbfb9f0");
    public override string Name => "Memory";
    public override PluginNamespace Namespace => PluginNamespace.Storage;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(MemoryStorageSpecifications);

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutOptions = options.ToObject<AboutOptions>();
        long totalSpace = 0, usedSpace = 0, freeSpace = 0;
        try
        {
            var client = new MemoryMetricsClient();
            var metrics = client.GetMetrics();

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

    public override async Task CreateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var createOptions = options.ToObject<CreateOptions>();
        await CreateEntityAsync(entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var writeOptions = options.ToObject<WriteOptions>();
        await WriteEntityAsync(entity, writeOptions, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var readOptions = options.ToObject<ReadOptions>();
        return await ReadEntityAsync(entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listptions = options.ToObject<ListOptions>();
        var deleteOptions = options.ToObject<DeleteOptions>();

        var dataTable = await FilteredEntitiesAsync(path, listptions, cancellationToken).ConfigureAwait(false);
        var entities = dataTable.CreateListFromTable();
        var storageEntities = entities.ToList();

        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list)
                continue;

            DeleteEntityAsync(list.Path);
        }

        if (deleteOptions.Purge is true)
        {
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
        }
    }

    public override async Task<bool> ExistAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        return await ExistEntityAsync(entity, options, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var listOptions = options.ToObject<ListOptions>();
        var filteredData = await FilteredEntitiesAsync(entity, listOptions, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task<TransferData> PrepareTransferring(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readOptions = options.ToObject<ReadOptions>();
        var listOptions = options.ToObject<ListOptions>();

        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var kindFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("Kind", StringComparison.OrdinalIgnoreCase));
        var fullPathFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

        if (!kindFieldExist)
            fields = fields.Append("Kind").ToArray();

        if (!fullPathFieldExist)
            fields = fields.Append("FullPath").ToArray();

        var dataFilterOptions = GetDataFilterOptions(listOptions);

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
                    var read = await ReadEntityAsync(entity, readOptions, cancellationToken).ConfigureAwait(false);
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
            PluginNamespace = Namespace,
            PluginType = Type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRows
        };

        return result;
    }

    public override async Task TransferAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, TransferData transferData, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (transferData.PluginNamespace == PluginNamespace.Storage)
        {
            var createOptions = options.ToObject<CreateOptions>();
            var writeOptions = options.ToObject<WriteOptions>();

            foreach (var item in transferData.Rows)
            {
                switch (item.Content)
                {
                    case null:
                    case "":
                        await CreateEntityAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                        break;
                    case var data:
                        var parentPath = PathHelper.GetParent(item.Key);
                        if (!PathHelper.IsRootPath(parentPath))
                        {
                            await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                            await WriteEntityAsync(item.Key, writeOptions, data, cancellationToken).ConfigureAwait(false);
                            _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                        }

                        break;
                }
            }
        }
        else
        {
            var path = PathHelper.ToUnixPath(entity);
            if (!string.IsNullOrEmpty(transferData.Content))
            {
                var fileBytes = Convert.FromBase64String(transferData.Content);
                await File.WriteAllBytesAsync(path, fileBytes, cancellationToken);
            }
            else
            {
                foreach (var item in transferData.Rows)
                {
                    if (item.Content != null)
                    {
                        var parentPath = PathHelper.GetParent(path);
                        var fileBytes = Convert.FromBase64String(item.Content);
                        await File.WriteAllBytesAsync(PathHelper.Combine(parentPath, item.Key), fileBytes, cancellationToken);
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
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
                continue;
            }
        }

        return compressEntries;
    }

    #region private methods
    private Task<List<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        var buckets = _entities.Keys;
        var result = buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket))
            .Select(bucket => bucket).ToList();

        return Task.FromResult(result);
    }

    private Task ListAsync(List<StorageEntity> result, string bucketName, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        if (!_entities.ContainsKey(bucketName))
            throw new Exception(string.Format(Resources.BucketNotExist, bucketName));

        var bucket = _entities[bucketName];

        foreach (var (key, value) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memEntity = bucket[key];
                result.Add(memEntity.ToEntity(bucketName, listOptions.IncludeMetadata));
            }
        }

        return Task.CompletedTask;
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

    private bool FolderExistAsync(string bucketName, string path)
    {
        if (!BucketExists(bucketName))
            return false;

        var bucket = _entities[bucketName];
        var folderPrefix = path + PathHelper.PathSeparator;
        return bucket.Keys.Any(x=>x.StartsWith(folderPrefix));
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
    
    private MemoryStorageBucketPathPart GetPartsAsync(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return new MemoryStorageBucketPathPart();

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

        return new MemoryStorageBucketPathPart(bucketName, relativePath);
    }


    private Task<string> AddFolder(string bucketName, string folderName)
    {
        var memoryEntity = new MemoryEntity(folderName);
        var bucket = _entities[bucketName];

        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        bucket[folderName] = memoryEntity;
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
        return Task.FromResult<string>(memoryEntity.Id);
    }

    private bool DeleteEntityAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = GetPartsAsync(path);
        var bucket = _entities[pathParts.BucketName];

        if (PathHelper.IsFile(path))
        {
            var isExist = ObjectExists(pathParts.BucketName, pathParts.RelativePath);
            if (!isExist)
                return false;

            var ms = bucket[pathParts.RelativePath];

            bucket.Remove(pathParts.RelativePath);
            return true;
        }

        var folderPrefix = !pathParts.RelativePath.EndsWith(PathHelper.PathSeparator) 
            ? pathParts.RelativePath + PathHelper.PathSeparator 
            : pathParts.RelativePath;

        var folderExist = bucket.Keys.Any(x => x.StartsWith(folderPrefix));

        if (!folderExist)
            return false;

        var itemToDelete = bucket.Keys.Where(x => x.StartsWith(folderPrefix)).Select(p=>p);
        foreach (var item in itemToDelete)
        {
            bucket.Remove(item);
        }
        return true;
    }

    private async Task CreateEntityAsync(string entity, CreateOptions options, 
        CancellationToken cancellationToken)
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

    private Task WriteEntityAsync(string entity, WriteOptions options, 
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
        var extension = Path.GetExtension(path);
        var bucket = _entities[pathParts.BucketName];
        var memoryEntity = new MemoryEntity(name, dataStream);
        bucket[pathParts.RelativePath] = memoryEntity;

        return Task.CompletedTask;
    }

    private Task<ReadResult> ReadEntityAsync(string entity, ReadOptions options, 
        CancellationToken cancellationToken)
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
            Content = memoryEntity.Content
        };

        return Task.FromResult(result);
    }

    private Task<bool> ExistEntityAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken)
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

    private async Task<DataTable> FilteredEntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        var storageEntities = await EntitiesAsync(path, options, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(options);
        var dataTable = storageEntities.ToDataTable();
        var result = _dataFilter.Filter(dataTable, dataFilterOptions);

        return result;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken)
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

            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(options.IncludeMetadata)));
            return storageEntities;
        }

        buckets.Add(pathParts.BucketName);

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(storageEntities, b, pathParts.RelativePath, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private DataFilterOptions GetDataFilterOptions(ListOptions options)
    {
        var fields = DeserializeToStringArray(options.Fields);
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

    private string[] DeserializeToStringArray(string? fields)
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