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
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;

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

    public Task<object> GetStatisticsAsync()
    {
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

    public Task CreateAsync(string entity, CreateOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (options.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        return Task.CompletedTask;
    }

    public Task WriteAsync(string entity, WriteOptions options, object dataOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && options.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        if (File.Exists(path) && options.Overwrite is true)
            DeleteAsync(path);

        using (var fileStream = File.Create(path))
        {
            dataStream.CopyTo(fileStream);
        }

        return Task.CompletedTask;
    }

    public Task<ReadResult> ReadAsync(string entity, ReadOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);
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

    public Task DeleteAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
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

    public Task PurgeAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        Directory.Delete(path, true);
        return Task.CompletedTask;
    }

    public Task<bool> ExistAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrWhiteSpace(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        return Task.FromResult(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    }


    public Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions listOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
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