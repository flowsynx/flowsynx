using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Extensions;

namespace FlowSynx.Plugins.LocalFileSystem.Services;

public class LocalFileManager : ILocalFileManager
{
    private readonly ILogger _logger;
    public LocalFileManager(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<object> About(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task Create(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task Delete(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Exist(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task<object> List(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<object> Read(PluginParameters parameters)
    {
        var pathParameter = parameters.ToObject<PathParameter>();
        return await ReadEntity(pathParameter.Path).ConfigureAwait(false);
    }

    public Task Rename(PluginParameters parameters)
    {
        throw new NotImplementedException();
    }

    public async Task Write(PluginParameters parameters)
    {
        var pathParameter = parameters.ToObject<PathParameter>();
        var writeParameter = parameters.ToObject<WriteParameter>();

        await WriteEntity(pathParameter.Path, writeParameter).ConfigureAwait(false);
    }

    //public Task<object> About()
    //{
    //    long totalSpace = 0, freeSpace = 0;
    //    try
    //    {
    //        foreach (var d in DriveInfo.GetDrives())
    //        {
    //            if (d is not { DriveType: DriveType.Fixed, IsReady: true }) continue;

    //            totalSpace += d.TotalSize;
    //            freeSpace += d.TotalFreeSpace;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex.Message);
    //        totalSpace = 0;
    //        freeSpace = 0;
    //    }

    //    var result = new
    //    {
    //        Total = totalSpace,
    //        Free = freeSpace,
    //        Used = (totalSpace - freeSpace)
    //    };

    //    return Task.FromResult<object>(result);
    //}

    //public async Task Create(PluginParameters parameters)
    //{
    //    var pathOptions = parameters.ToObject<PathOptions>();
    //    var createOptions = parameters.ToObject<CreateOptions>();

    //    await CreateEntity(pathOptions.Path, createOptions).ConfigureAwait(false);
    //}

    //public async Task Write(PluginParameters parameters)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var writeOptions = context.Options.ToObject<WriteOptions>();

    //    if (context.Data != null && context.Data.Any())
    //        await WriteEntityFromData(pathOptions.Path, writeOptions, context.Data).ConfigureAwait(false);
    //    else
    //        await WriteEntity(pathOptions.Path, writeOptions).ConfigureAwait(false);
    //}

    //public async Task<InterchangeData> Read(PluginParameters parameters)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var readOptions = context.Options.ToObject<ReadOptions>();

    //    return await ReadEntity(pathOptions.Path, readOptions).ConfigureAwait(false);
    //}

    //public Task Rename(PluginParameters parameters)
    //{
    //    throw new NotImplementedException();
    //}

    //public async Task Delete(PluginParameters parameters)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();
    //    var deleteOptions = context.Options.ToObject<DeleteOptions>();

    //    var path = PathHelper.ToUnixPath(pathOptions.Path);
    //    listOptions.Fields = null;

    //    var filteredEntities = await FilteredEntitiesList(path, listOptions).ConfigureAwait(false);

    //    var entityItems = filteredEntities.Rows;
    //    if (entityItems.Count <= 0)
    //        throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

    //    foreach (DataRow entityItem in entityItems)
    //        await DeleteEntity(entityItem["FullPath"].ToString());

    //    if (deleteOptions.Purge is true)
    //        await PurgeEntity(path);
    //}

    //public async Task<bool> Exist(Context context)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    return await ExistEntity(pathOptions.Path).ConfigureAwait(false);
    //}

    //public async Task<InterchangeData> FilteredEntities(Context context)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();

    //    return await FilteredEntitiesList(pathOptions.Path, listOptions).ConfigureAwait(false);
    //}

    //public Task Transfer(Context context, CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}

    ////public async Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    ////    TransferKind transferKind, CancellationToken cancellationToken)
    ////{
    ////    var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
    ////    var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
    ////    var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

    ////    var transferData = await PrepareDataForTransferring(@namespace, type, sourcePathOptions.Path,
    ////        sourceListOptions, sourceReadOptions);

    ////    var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

    ////    foreach (var row in transferData.Rows)
    ////        row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

    ////    await destinationContext.ConnectorContext.Current.ProcessTransfer(destinationContext, transferData, transferKind, cancellationToken);
    ////}

    ////public async Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    ////    CancellationToken cancellationToken)
    ////{
    ////    var pathOptions = context.Options.ToObject<PathOptions>();
    ////    var createOptions = context.Options.ToObject<CreateOptions>();
    ////    var writeOptions = context.Options.ToObject<WriteOptions>();

    ////    var path = PathHelper.ToUnixPath(pathOptions.Path);

    ////    if (!string.IsNullOrEmpty(transferData.Content))
    ////    {
    ////        var parentPath = PathHelper.GetParent(path);
    ////        if (!PathHelper.IsRootPath(parentPath))
    ////        {
    ////            var newWriteOption = new WriteOptions
    ////            {
    ////                Data = transferData.Content,
    ////                Overwrite = writeOptions.Overwrite
    ////            };

    ////            await CreateEntity(parentPath, createOptions).ConfigureAwait(false);
    ////            await WriteEntity(path, newWriteOption).ConfigureAwait(false);
    ////            _logger.LogInformation($"Copy operation done for entity '{path}'");
    ////        }
    ////    }
    ////    else
    ////    {
    ////        foreach (var item in transferData.Rows)
    ////        {
    ////            if (string.IsNullOrEmpty(item.Content))
    ////            {
    ////                if (transferData.Namespace == Namespace.Storage)
    ////                {
    ////                    await CreateEntity(item.Key, createOptions).ConfigureAwait(false);
    ////                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    ////                }
    ////            }
    ////            else
    ////            {
    ////                var newPath = item.Key;
    ////                if (transferData.Namespace != Namespace.Storage)
    ////                {
    ////                    newPath = Path.Combine(path, item.Key);
    ////                }

    ////                var parentPath = PathHelper.GetParent(newPath);
    ////                if (!PathHelper.IsRootPath(parentPath))
    ////                {
    ////                    var newWriteOption = new WriteOptions
    ////                    {
    ////                        Data = item.Content,
    ////                        Overwrite = writeOptions.Overwrite,
    ////                    };

    ////                    await CreateEntity(parentPath, createOptions).ConfigureAwait(false);
    ////                    await WriteEntity(newPath, newWriteOption).ConfigureAwait(false);
    ////                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
    ////                }
    ////            }
    ////        }
    ////    }
    ////}

    //public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();
    //    var path = PathHelper.ToUnixPath(pathOptions.Path);
    //    var storageEntities = await EntitiesList(path, listOptions);

    //    var entityItems = storageEntities.ToList();
    //    if (!entityItems.Any())
    //        throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

    //    var compressEntries = new List<CompressEntry>();
    //    foreach (var entityItem in entityItems)
    //    {
    //        if (!string.Equals(entityItem.Kind, StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
    //        {
    //            _logger.LogWarning($"The item '{entityItem.Name}' is not a file.");
    //            continue;
    //        }

    //        try
    //        {
    //            var readOptions = new ReadOptions { Hashing = false };
    //            var content = await ReadEntity(entityItem.FullPath, readOptions).ConfigureAwait(false);
    //            compressEntries.Add(new CompressEntry
    //            {
    //                Name = entityItem.Name,
    //                ContentType = entityItem.ContentType,
    //                Content = (byte[])content.Rows[0]["Content"],
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogWarning(ex.Message);
    //        }
    //    }

    //    return compressEntries;
    //}

    //#region internal methods
    //private Task CreateEntity(string path, CreateOptions options)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    if (string.IsNullOrEmpty(path))
    //        throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

    //    if (!PathHelper.IsDirectory(path))
    //        throw new StorageException(Resources.ThePathIsNotDirectory);

    //    var directory = Directory.CreateDirectory(path);
    //    if (options.Hidden is true)
    //        directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

    //    return Task.CompletedTask;
    //}

    private Task WriteEntity(string path, WriteParameter parameters)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        if (File.Exists(path) && parameters.Overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = parameters.Data;
        if (dataValue is not string data)
            throw new Exception(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        if (File.Exists(path) && parameters.Overwrite is true)
            DeleteEntity(path);

        using (var fileStream = File.Create(path))
        {
            dataStream.CopyTo(fileStream);
        }

        return Task.CompletedTask;
    }

    //private Task WriteEntityFromData(string path, WriteOptions options, List<object> interchangedData)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    if (string.IsNullOrEmpty(path))
    //        throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

    //    //if (!PathHelper.IsDirectory(path))
    //    //    throw new StorageException(Resources.ThePathIsNotDirectory);

    //    //if (File.Exists(path) && overwrite is false)
    //    //    throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

    //    foreach (var item in interchangedData)
    //    {
    //        if (item is InterchangeData interchange)
    //        {
    //            if (!string.IsNullOrEmpty(interchange.Metadata.Content))
    //            {
    //                if (!PathHelper.IsFile(path))
    //                    throw new StorageException(Resources.ThePathIsNotFile);

    //                if (File.Exists(path) && options.Overwrite is false)
    //                    throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

    //                var parentPath = PathHelper.GetParent(path);
    //                Directory.CreateDirectory(parentPath);

    //                var dataStream = interchange.Metadata.Content.IsBase64String() ? 
    //                    interchange.Metadata.Content.Base64ToStream() : 
    //                    interchange.Metadata.Content.ToStream();

    //                if (File.Exists(path) && options.Overwrite is true)
    //                    DeleteEntity(path);

    //                using (var fileStream = File.Create(path))
    //                {
    //                    dataStream.CopyTo(fileStream);
    //                }
    //            }
    //            else
    //            {
    //                Directory.CreateDirectory(path);

    //                foreach (InterchangeRow row in interchange.Rows)
    //                {
    //                    var newPath = PathHelper.Combine(path, row.Metadata.Key);
    //                    if (!string.IsNullOrEmpty(row.Metadata.Content))
    //                    {
    //                        var dataStream = row.Metadata.Content.IsBase64String() ?
    //                            row.Metadata.Content.Base64ToStream() :
    //                            row.Metadata.Content.ToStream();

    //                        if (File.Exists(newPath) && options.Overwrite is true)
    //                            DeleteEntity(newPath);

    //                        using (var fileStream = File.Create(newPath))
    //                        {
    //                            dataStream.CopyTo(fileStream);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        Directory.CreateDirectory(newPath);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    return Task.CompletedTask;
    //}

    private Task<string> ReadEntity(string path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var content = File.ReadAllBytes(path);

        return Task.FromResult(content.ToBase64String());
    }

    //private async Task<InterchangeData> ReadEntity(string path, ReadOptions options)
    //{
    //    var result = new InterchangeData();
    //    var (content, contentHash) = await ReadEntityBytes(path, options).ConfigureAwait(false);
    //    result.Metadata.Content = content;

    //    var row = result.NewRow();
    //    row.Metadata.ContentHash = contentHash;

    //    return result;
    //}

    private Task DeleteEntity(string? path)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

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

    //private Task PurgeEntity(string? path)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    var directoryInfo = new DirectoryInfo(path);
    //    if (!directoryInfo.Exists)
    //        throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

    //    Directory.Delete(path, true);
    //    return Task.CompletedTask;
    //}

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

    //private Task<bool> ExistEntity(string path)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    if (string.IsNullOrWhiteSpace(path))
    //        throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

    //    return Task.FromResult(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    //}

    //private async Task<InterchangeData> FilteredEntitiesList(string path, ListOptions listOptions)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    var entities = await EntitiesList(path, listOptions);

    //    var dataFilterOptions = GetDataTableOption(listOptions);
    //    var dataTable = entities.ListToInterchangeData();

    //    foreach (InterchangeRow row in dataTable.Rows)
    //    {
    //        row.Metadata.Key = row["Name"].ToString();
    //        row.Metadata.ContentType = row["ContentType"].ToString();

    //        if (PathHelper.IsFile(row["FullPath"].ToString()))
    //        {
    //            var (content, contentHash) =
    //            await ReadEntityBytes(row["FullPath"].ToString(), new ReadOptions { Hashing = false })
    //            .ConfigureAwait(false);

    //            row.Metadata.Content = content;
    //            row.Metadata.ContentHash = contentHash;
    //        }
    //    }

    //    var filteredEntities = _dataService.Select(dataTable, dataFilterOptions);
    //    return filteredEntities;
    //}

    //private Task<IEnumerable<StorageEntity>> EntitiesList(string path, ListOptions listOptions)
    //{
    //    path = PathHelper.ToUnixPath(path);
    //    if (string.IsNullOrEmpty(path))
    //        throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

    //    if (!PathHelper.IsDirectory(path))
    //        throw new StorageException(Resources.ThePathIsNotDirectory);

    //    if (!Directory.Exists(path))
    //        throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

    //    var storageEntities = new List<StorageEntity>();
    //    var directoryInfo = new DirectoryInfo(path);

    //    storageEntities.AddRange(directoryInfo.FindFiles("*", listOptions.Recurse)
    //        .Select(file => file.ToEntity(listOptions.IncludeMetadata)));

    //    storageEntities.AddRange(directoryInfo.FindDirectories("*", listOptions.Recurse)
    //        .Select(dir => dir.ToEntity(listOptions.IncludeMetadata)));

    //    return Task.FromResult<IEnumerable<StorageEntity>>(storageEntities);
    //}

    ////private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path,
    ////   ListOptions listOptions, ReadOptions readOptions)
    ////{
    ////    path = PathHelper.ToUnixPath(path);

    ////    var storageEntities = await EntitiesList(path, listOptions);

    ////    var fields = GetFields(listOptions.Fields);
    ////    var kindFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("Kind", StringComparison.OrdinalIgnoreCase));
    ////    var fullPathFieldExist = fields.Count == 0 || fields.Any(s => s.Name.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

    ////    if (!kindFieldExist)
    ////        fields.Append("Kind");

    ////    if (!fullPathFieldExist)
    ////        fields.Append("FullPath");

    ////    var dataFilterOptions = GetDataTableOption(listOptions);

    ////    var dataTable = storageEntities.ListToDataTable();
    ////    var filteredData = _dataService.Select(dataTable, dataFilterOptions);
    ////    var transferDataRows = new List<TransferDataRow>();

    ////    foreach (DataRow row in filteredData.Rows)
    ////    {
    ////        var content = string.Empty;
    ////        var contentType = string.Empty;
    ////        var fullPath = row["FullPath"].ToString() ?? string.Empty;

    ////        if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
    ////        {
    ////            if (!string.IsNullOrEmpty(fullPath))
    ////            {
    ////                var read = await ReadEntity(fullPath, readOptions).ConfigureAwait(false);
    ////                content = read.Content.ToBase64String();
    ////            }
    ////        }

    ////        if (!kindFieldExist)
    ////            row["Kind"] = DBNull.Value;

    ////        if (!fullPathFieldExist)
    ////            row["FullPath"] = DBNull.Value;

    ////        var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
    ////        transferDataRows.Add(new TransferDataRow
    ////        {
    ////            Key = fullPath,
    ////            ContentType = contentType,
    ////            Content = content,
    ////            Items = itemArray
    ////        });
    ////    }

    ////    if (!kindFieldExist)
    ////        filteredData.Columns.Remove("Kind");

    ////    if (!fullPathFieldExist)
    ////        filteredData.Columns.Remove("FullPath");

    ////    var result = new TransferData
    ////    {
    ////        Namespace = @namespace,
    ////        ConnectorType = type,
    ////        Columns = GetTransferDataColumn(filteredData),
    ////        Rows = transferDataRows
    ////    };

    ////    return result;
    ////}

    //private SelectDataOption GetDataTableOption(ListOptions options) => new()
    //{
    //    Fields = GetFields(options.Fields),
    //    Filter = GetFilterList(options.Filter),
    //    Sort = GetSortList(options.Sort),
    //    CaseSensitive = options.CaseSensitive,
    //    Paging = GetPaging(options.Paging),
    //};

    //private FieldsList GetFields(string? json)
    //{
    //    var result = new FieldsList();
    //    if (!string.IsNullOrEmpty(json))
    //    {
    //        result = _deserializer.Deserialize<FieldsList>(json);
    //    }

    //    return result;
    //}

    //private FilterList GetFilterList(string? json)
    //{
    //    var result = new FilterList();
    //    if (!string.IsNullOrEmpty(json))
    //    {
    //        result = _deserializer.Deserialize<FilterList>(json);
    //    }

    //    return result;
    //}

    //private SortList GetSortList(string? json)
    //{
    //    var result = new SortList();
    //    if (!string.IsNullOrEmpty(json))
    //    {
    //        result = _deserializer.Deserialize<SortList>(json);
    //    }

    //    return result;
    //}

    //private Paging GetPaging(string? json)
    //{
    //    var result = new Paging();
    //    if (!string.IsNullOrEmpty(json))
    //    {
    //        result = _deserializer.Deserialize<Paging>(json);
    //    }

    //    return result;
    //}

    ////private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    ////{
    ////    return dataTable.Columns.Cast<DataColumn>()
    ////        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    ////}
    //#endregion
}