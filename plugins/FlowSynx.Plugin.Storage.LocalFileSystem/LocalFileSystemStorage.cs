using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Net;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Security;
using FlowSynx.Plugin.Storage.Options;
using FlowSynx.IO.Compression;
using System.Data;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

public class LocalFileSystemStorage : PluginBase
{
    private readonly ILogger<LocalFileSystemStorage> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;

    public LocalFileSystemStorage(ILogger<LocalFileSystemStorage> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public override string Name => "LocalFileSystem";
    public override PluginNamespace Namespace => PluginNamespace.Storage;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutOptions = options.ToObject<AboutOptions>();
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

    public override Task CreateAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (createOptions.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        return Task.CompletedTask;
    }

    public override Task WriteAsync(string entity, PluginOptions? options, 
        object dataOptions, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && writeOptions.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();
        
        if (File.Exists(path) && writeOptions.Overwrite is true)
            DeleteEntityAsync(path);

        using (var fileStream = File.Create(path))
        {
            dataStream.CopyTo(fileStream);
        }

        return Task.CompletedTask;
    }

    public override Task<object> ReadAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readOptions = options.ToObject<ReadOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var file = new FileInfo(path);
        var fileExtension = file.Extension;
        var result = new StorageRead()
        {
            Stream = new StorageStream(File.OpenRead(path)),
            ContentType = fileExtension.GetContentType(),
            Extension = fileExtension,
            Md5 = HashHelper.Md5.GetHash(file)
        };

        return Task.FromResult<object>(result);
    }

    public override Task UpdateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteOptions = options.ToObject<DeleteOptions>();
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

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
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            Directory.Delete(path, true);
        }
    }

    public override Task<bool> ExistAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        return Task.FromResult<bool>(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await ListEntitiesAsync(path, listOptions, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = storageEntities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    public override async Task<TransferData> PrepareTransferring(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await ListEntitiesAsync(path, listOptions, cancellationToken);
        
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
                    var read = await ReadAsync(fullPath, null, cancellationToken);
                    if (read is StorageRead storageRead)
                    {
                        content = storageRead.Stream.ConvertToBase64();
                        contentType = storageRead.ContentType;
                    }
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
            State = TransferState.Copy,
            Columns = columnNames,
            Rows = transferDataRows
        };

        return result;
    }

    public override async Task TransferAsync(string entity, PluginOptions? options,
        TransferData transferData, CancellationToken cancellationToken = new CancellationToken())
    {
        if (transferData.PluginNamespace == PluginNamespace.Storage)
        {
            foreach (var item in transferData.Rows)
            {
                switch (item.Content)
                {
                    case null:
                    case "":
                        await CreateAsync(item.Key, options, cancellationToken);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                        break;
                    case var data:
                        var parentPath = PathHelper.GetParent(item.Key);
                        if (!PathHelper.IsRootPath(parentPath))
                        {
                            await CreateAsync(parentPath, options, cancellationToken);
                            await WriteAsync(item.Key, options, data, cancellationToken);
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

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await ListEntitiesAsync(path, listOptions, cancellationToken);

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
                var stream = await ReadAsync(entityItem.FullPath, options, cancellationToken);
                if (stream is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entityItem.Name}' could be not read.");
                    continue;
                }

                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
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

    #region internal methods
    private bool DeleteEntityAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (PathHelper.IsDirectory(path))
        {
            if (!Directory.Exists(path)) 
                return false;

            DeleteAllEntities(path);
            Directory.Delete(path);
        }
        else
        {
            if (!File.Exists(path))
                return false;
            
            File.Delete(path);
        }
        return true;
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

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private Task<IEnumerable<StorageEntity>> ListEntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken = new CancellationToken())
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

        storageEntities.AddRange(directoryInfo.FindFiles("*", options.Recurse)
            .Select(file => file.ToEntity(options.IncludeMetadata)));

        storageEntities.AddRange(directoryInfo.FindDirectories("*", options.Recurse)
            .Select(dir => dir.ToEntity(options.IncludeMetadata)));
        
       return Task.FromResult<IEnumerable<StorageEntity>>(storageEntities);
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
    #endregion
}