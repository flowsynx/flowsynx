using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO;
using EnsureThat;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Security;
using FlowSynx.Connectors.Storage.LocalFileSystem.Extensions;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public class LocalFileManager : ILocalFileManager
{
    private readonly ILogger _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    public LocalFileManager(ILogger logger, IDataFilter dataFilter, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public Task<object> About(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        long totalSpace = 0, freeSpace = 0;
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d is not { DriveType: DriveType.Fixed, IsReady: true }) continue;

                totalSpace += d.TotalSize;
                freeSpace += d.TotalFreeSpace;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            freeSpace = 0;
        }

        var result = new
        {
            Total = totalSpace,
            Free = freeSpace,
            Used = (totalSpace - freeSpace)
        };

        return Task.FromResult<object>(result);
    }

    public async Task CreateAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        await CreateEntityAsync(pathOptions.Path, createOptions).ConfigureAwait(false);
    }

    public async Task WriteAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        await WriteEntityAsync(pathOptions.Path, writeOptions).ConfigureAwait(false);
    }

    public async Task<ReadResult> ReadAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        return await ReadEntityAsync(pathOptions.Path, readOptions).ConfigureAwait(false);
    }

    public Task UpdateAsync(Context context)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        listOptions.Fields = null;

        var filteredEntities = await FilteredEntitiesListAsync(path, listOptions).ConfigureAwait(false);

        var entityItems = filteredEntities.Rows;
        if (entityItems.Count <= 0)
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (DataRow entityItem in entityItems)
            await DeleteEntityAsync(entityItem["FullPath"].ToString());

        if (deleteOptions.Purge is true)
            await PurgeEntityAsync(path);
    }

    public async Task<bool> ExistAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();

        return await ExistEntityAsync(pathOptions.Path).ConfigureAwait(false);
    }

    public async Task<IEnumerable<object>> FilteredEntitiesAsync(Context context)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var result = await FilteredEntitiesListAsync(pathOptions.Path, listOptions).ConfigureAwait(false);
        return result.CreateListFromTable();
    }

    public async Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
        var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
        var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

        var transferData = await PrepareDataForTransferring(@namespace, type, sourcePathOptions.Path,
            sourceListOptions, sourceReadOptions);

        var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

        foreach (var row in transferData.Rows)
            row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public async Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken)
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
                var newWriteOption = new WriteOptions
                {
                    Data = transferData.Content,
                    Overwrite = writeOptions.Overwrite
                };

                await CreateEntityAsync(parentPath, createOptions).ConfigureAwait(false);
                await WriteEntityAsync(path, newWriteOption).ConfigureAwait(false);
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
                        await CreateEntityAsync(item.Key, createOptions).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var newPath = item.Key;
                    if (transferData.Namespace != Namespace.Storage)
                    {
                        newPath = Path.Combine(path, item.Key);
                    }

                    var parentPath = PathHelper.GetParent(newPath);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        var newWriteOption = new WriteOptions
                        {
                            Data = item.Content,
                            Overwrite = writeOptions.Overwrite,
                        };

                        await CreateEntityAsync(parentPath, createOptions).ConfigureAwait(false);
                        await WriteEntityAsync(newPath, newWriteOption).ConfigureAwait(false);
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
        var listOptions = context.Options.ToObject<ListOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        var storageEntities = await EntitiesListAsync(path, listOptions);

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
                var content = await ReadEntityAsync(entityItem.FullPath, readOptions).ConfigureAwait(false);
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

    #region internal methods
    private Task CreateEntityAsync(string path, CreateOptions options)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (options.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        return Task.CompletedTask;
    }

    private Task WriteEntityAsync(string path, WriteOptions options)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && options.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = options.Data.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        if (File.Exists(path) && options.Overwrite is true)
            DeleteEntityAsync(path);

        using (var fileStream = File.Create(path))
        {
            dataStream.CopyTo(fileStream);
        }

        return Task.CompletedTask;
    }

    private Task<ReadResult> ReadEntityAsync(string path, ReadOptions options)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var file = new FileInfo(path);

        var result = new ReadResult
        {
            Content = File.ReadAllBytes(path),
            ContentHash = HashHelper.Md5.GetHash(file)
        };

        return Task.FromResult(result);
    }

    private Task DeleteEntityAsync(string? path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (PathHelper.IsDirectory(path))
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning($"The specified path '{path}' is not exist.");
                return Task.CompletedTask;
            }

            DeleteAllEntities(path);
            Directory.Delete(path);
            _logger.LogInformation($"The specified path '{path}' was deleted successfully.");
        }
        else
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning($"The specified path '{path}' is not exist.");
                return Task.CompletedTask;
            }

            File.Delete(path);
            _logger.LogInformation($"The specified path '{path}' was deleted successfully.");
        }

        return Task.CompletedTask;
    }

    private Task PurgeEntityAsync(string? path)
    {
        path = PathHelper.ToUnixPath(path);
        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        Directory.Delete(path, true);
        return Task.CompletedTask;
    }

    private void DeleteAllEntities(string path)
    {
        var di = new DirectoryInfo(path);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    private Task<bool> ExistEntityAsync(string path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        return Task.FromResult(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    }

    private async Task<DataTable> FilteredEntitiesListAsync(string path, ListOptions listOptions)
    {
        path = PathHelper.ToUnixPath(path);
        var entities = await EntitiesListAsync(path, listOptions);

        var dataFilterOptions = GetFilterOptions(listOptions);
        var dataTable = entities.ToDataTable();
        var filteredEntities = _dataFilter.Filter(dataTable, dataFilterOptions);

        return filteredEntities;
    }

    private Task<IEnumerable<StorageEntity>> EntitiesListAsync(string path, ListOptions listOptions)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        if (!Directory.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var storageEntities = new List<StorageEntity>();
        var directoryInfo = new DirectoryInfo(path);

        storageEntities.AddRange(directoryInfo.FindFiles("*", listOptions.Recurse)
            .Select(file => file.ToEntity(listOptions.IncludeMetadata)));

        storageEntities.AddRange(directoryInfo.FindDirectories("*", listOptions.Recurse)
            .Select(dir => dir.ToEntity(listOptions.IncludeMetadata)));

        return Task.FromResult<IEnumerable<StorageEntity>>(storageEntities);
    }

    private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path,
       ListOptions listOptions, ReadOptions readOptions)
    {
        path = PathHelper.ToUnixPath(path);

        var storageEntities = await EntitiesListAsync(path, listOptions);

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
                    var read = await ReadEntityAsync(fullPath, readOptions).ConfigureAwait(false);
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

    private DataFilterOptions GetFilterOptions(ListOptions options)
    {
        var fields = GetFields(options.Fields);
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
    #endregion
}