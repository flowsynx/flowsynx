﻿using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Google.Drive.Extensions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Google.Drive.Models;
using Google;
using System.Net;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Net;
using Google.Apis.Upload;
using FlowSynx.Connectors.Abstractions;
using EnsureThat;
using FlowSynx.IO.Serialization;
using System.Data;
using FlowSynx.IO.Compression;
using FlowSynx.Data;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Storage.Google.Drive.Services;

internal class GoogleDriveManager : IGoogleDriveManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly DriveService _client;
    private readonly string _rootFolderId;
    private readonly Dictionary<string, string> _pathDictionary;
    private readonly GoogleDriveSpecifications? _specifications;

    public GoogleDriveManager(ILogger logger, DriveService client, GoogleDriveSpecifications? specifications,
        IDataService dataService, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(specifications, nameof(specifications));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _client = client;
        _specifications = specifications;
        _dataService = dataService;
        _deserializer = deserializer;
        _rootFolderId = specifications.FolderId;
        _pathDictionary = new Dictionary<string, string> { { PathHelper.PathSeparatorString, _rootFolderId }, { "", _rootFolderId } };
    }

    public async Task<object> About(Context context, CancellationToken cancellationToken)
    {
        long totalSpace = 0, totalUsed, totalFree = 0;
        try
        {
            var request = _client.About.Get();
            request.Fields = "storageQuota";
            var statistics = await request.ExecuteAsync(cancellationToken);

            totalUsed = statistics.StorageQuota.UsageInDrive ?? 0;
            if (statistics.StorageQuota.Limit is > 0)
            {
                totalSpace = statistics.StorageQuota.Limit.Value;
                totalFree = totalSpace - totalUsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            totalUsed = 0;
            totalFree = 0;
        }

        return new
        {
            Total = totalSpace,
            Free = totalFree,
            Used = totalUsed
        };
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

    public Task Update(Context context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        listOptions.Fields = null;

        var filteredEntities = await FilteredEntitiesList(path, listOptions, cancellationToken).ConfigureAwait(false);

        var entityItems = filteredEntities.Rows;
        if (entityItems.Count <= 0)
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (DataRow entityItem in entityItems)
            await DeleteEntity(entityItem["FullPath"].ToString(), cancellationToken).ConfigureAwait(false);

        if (deleteOptions.Purge is true)
            await PurgeEntity(path, cancellationToken);
    }

    public async Task<bool> Exist(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();

        return await ExistEntity(pathOptions.Path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InterchangeData> FilteredEntities(Context context,
    CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var result = await FilteredEntitiesList(pathOptions.Path, listOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public Task Transfer(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken)
    //{
    //    var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
    //    var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
    //    var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

    //    var transferData = await PrepareDataForTransferring(@namespace, type, sourcePathOptions.Path,
    //        sourceListOptions, sourceReadOptions, cancellationToken);

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

    //            await CreateEntity(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
    //            await WriteEntity(path, newWriteOption, cancellationToken).ConfigureAwait(false);
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
    //                    var newWriteOption = new WriteOptions
    //                    {
    //                        Data = item.Content,
    //                        Overwrite = writeOptions.Overwrite,
    //                    };

    //                    await CreateEntity(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
    //                    await WriteEntity(item.Key, newWriteOption, cancellationToken).ConfigureAwait(false);
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

    private IEnumerable<string> Fields => new List<string>()
    {
        "id",
        "name",
        "parents",
        "size",
        "mimeType",
        "createdTime",
        "modifiedTime",
        "md5Checksum",
        "copyRequiresWriterPermission",
        "description",
        "folderColorRgb",
        "starred",
        "viewedByMe",
        "viewedByMeTime",
        "writersCanShare"
    };

    private async Task CreateEntity(string path, CreateOptions options, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = PathHelper.Split(path);
        var folderName = pathParts.Last();
        var parentFolder = string.Join(PathHelper.PathSeparatorString, pathParts.SkipLast(1));
        var folder = await GetDriveFolder(parentFolder, cancellationToken).ConfigureAwait(false);

        if (!folder.Exist)
            throw new StorageException(string.Format(Resources.ParentPathIsNotExist, parentFolder));

        var driveFolder = new DriveFile
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { folder.Id }
        };

        var command = _client.Files.Create(driveFolder);
        await command.ExecuteAsync(cancellationToken);
        _logger.LogInformation($"Directory '{folderName}' was created successfully.");
    }

    private async Task WriteEntity(string path, WriteOptions options, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = options.Data.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (file.Exist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, path));

            var directoryPath = PathHelper.GetParent(path);
            var folderId = await GetFolderId(directoryPath, cancellationToken).ConfigureAwait(false);

            var fileMime = Path.GetExtension(fileName).GetContentType();
            var driveFile = new DriveFile
            {
                Name = fileName,
                MimeType = fileMime,
                Parents = new[] { folderId }
            };

            var request = _client.Files.Create(driveFile, dataStream, fileMime);
            var response = await request.UploadAsync(cancellationToken).ConfigureAwait(false);
            if (response.Status != UploadStatus.Completed)
                throw response.Exception;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<InterchangeData> ReadEntity(string path, ReadOptions options, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (!file.Exist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var ms = new MemoryStream();
            var request = _client.Files.Get(file.Id);
            request.Fields = "id, name, mimeType, md5Checksum";
            var fileRequest = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            request.Download(ms);

            ms.Position = 0;

            var result = new InterchangeData();
            result.Columns.Add("Content", typeof(byte[]));

            var row = result.NewRow();
            row.Metadata.ContentHash = fileRequest.Md5Checksum;
            row["Content"] = ms.ToArray();

            return result;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task DeleteEntity(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var file = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
                if (!file.Exist)
                {
                    _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                    return;
                }

                var command = _client.Files.Delete(file.Id);
                await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            if (!folder.Exist) return;

            if (PathHelper.IsRootPath(path))
                throw new StorageException(Resources.CanNotPurgeRootDirectory);

            await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
            DeleteFolderId(path);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
        }
    }

    private async Task PurgeEntity(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
        if (folder.Exist)
        {
            if (!PathHelper.IsRootPath(path))
            {
                await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
                DeleteFolderId(path);
            }
            else
            {
                _logger.LogWarning($"The path {path} is root path and can't be purged!");
            }
        }
        else
        {
            _logger.LogWarning($"The path {path} is not exist!");
        }
    }

    private async Task<bool> ExistEntity(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var fileExist = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
                return fileExist.Exist;
            }

            var folderExist = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            return folderExist.Exist;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<InterchangeData> FilteredEntitiesList(string path, 
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var entities = await EntitiesList(path, listOptions, cancellationToken);

        var dataFilterOptions = GetDataTableOption(listOptions);
        var dataTable = entities.ListToInterchangeData();
        var filteredEntities = _dataService.Select(dataTable, dataFilterOptions);

        return (InterchangeData)filteredEntities;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesList(string path, 
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);

        if (_specifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = await ListObjects(path, listOptions, cancellationToken).ConfigureAwait(false);

        return storageEntities;
    }

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path, 
    //    ListOptions listOptions, ReadOptions readOptions, CancellationToken cancellationToken = default)
    //{
    //    path = PathHelper.ToUnixPath(path);

    //    var storageEntities = await EntitiesList(path, listOptions, cancellationToken);

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
    //                var read = await ReadEntity(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
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

    private async Task<GoogleDrivePathPart> GetDriveFolder(string path, CancellationToken cancellationToken)
    {
        try
        {
            var folderId = await GetFolderId(path, cancellationToken).ConfigureAwait(false);
            return new GoogleDrivePathPart(!string.IsNullOrEmpty(folderId), folderId);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePathPart(false, string.Empty);
        }
    }

    private async Task<GoogleDrivePathPart> GetDriveFile(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, filePath));

            var directoryPath = PathHelper.GetParent(filePath) + PathHelper.PathSeparatorString;
            var folderId = await GetFolderId(directoryPath, cancellationToken).ConfigureAwait(false);

            var listRequest = _client.Files.List();
            listRequest.Q = $"('{folderId}' in parents) and (name='{fileName}') and (trashed=false)";
            var files = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return !files.Files.Any()
                ? new GoogleDrivePathPart(false, string.Empty)
                : new GoogleDrivePathPart(true, files.Files.First().Id);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePathPart(false, string.Empty);
        }
    }

    private async Task<List<StorageEntity>> ListObjects(string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        await ListFolder(result, path, listOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task ListFolder(List<StorageEntity> entities, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        var folderId = await GetFolderId(path, cancellationToken);
        var request = _client.Files.List();
        request.Q = $"'{folderId}' in parents and (trashed=false)";
        request.Fields = $"nextPageToken, files({string.Join(",", Fields)})";

        do
        {
            var files = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (files != null)
            {
                foreach (var item in files.Files)
                {
                    if (item == null)
                        continue;

                    result.Add(item.MimeType == "application/vnd.google-apps.folder"
                        ? item.ToEntity(path, true, listOptions.IncludeMetadata)
                        : item.ToEntity(path, false, listOptions.IncludeMetadata));
                }
            }

            request.PageToken = files?.NextPageToken;
        }
        while (request.PageToken != null);

        entities.AddRange(result);

        if (listOptions.Recurse)
        {
            var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(directories.Select(dir => ListFolder(entities, dir.FullPath,
                listOptions, cancellationToken))).ConfigureAwait(false);
        }
    }

    private async Task<string> GetFolderId(string path, CancellationToken cancellationToken)
    {
        if (_pathDictionary.ContainsKey(path))
            return _pathDictionary[path];

        var folderId = _rootFolderId;
        var route = string.Empty;
        var queue = new Queue<string>();
        var pathParts = PathHelper.Split(path);
        foreach (var subPath in pathParts)
        {
            queue.Enqueue(subPath);
        }

        while (queue.Count > 0)
        {
            var enqueuePath = queue.Dequeue();
            var request = _client.Files.List();

            request.Q = $"('{folderId}' in parents) " +
                        $"and (mimeType = 'application/vnd.google-apps.folder') " +
                        $"and (name = '{enqueuePath}') " +
                        $"and (trashed=false)";

            request.Fields = "nextPageToken, files(id, name)";

            var fileListResponse = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (fileListResponse is null || fileListResponse.Files.Count <= 0)
            {
                _logger.LogWarning("The entered path could not be found!");
                return string.Empty;
            }

            var file = fileListResponse.Files.First();
            folderId = file.Id;
            route = PathHelper.Combine(route, file.Name);

            if (!route.EndsWith(PathHelper.PathSeparator))
                route += PathHelper.PathSeparator;

            if (!_pathDictionary.ContainsKey(route))
                _pathDictionary.Add(route, file.Id);
        }

        return folderId;
    }

    private void DeleteFolderId(string path)
    {
        if (_pathDictionary.ContainsKey(path))
            _pathDictionary.Remove(path);
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
