﻿using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.Connectors.Storage.Azure.Blobs.Extensions;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;
using FlowSynx.Connectors.Storage.Exceptions;
using EnsureThat;
using FlowSynx.IO.Serialization;
using System.Data;
using FlowSynx.IO.Compression;
using FlowSynx.Data;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public class AzureBlobManager : IAzureBlobManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly BlobServiceClient _client;

    public AzureBlobManager(ILogger logger, BlobServiceClient client, IDataService dataService, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _client = client;
        _dataService = dataService;
        _deserializer = deserializer;
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

    public async Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken)
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
    ////}

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
    //                Overwrite = writeOptions.Overwrite,
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
                var content = await ReadEntity(entityItem.FullPath, readOptions, cancellationToken);
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

        try
        {
            var pathParts = GetParts(path);
            var container = _client.GetBlobContainerClient(pathParts.ContainerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(pathParts.RelativePath))
                _logger.LogWarning($"The Azure Blob storage doesn't support create empty directory.");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new StorageException(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
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
            var pathParts = GetParts(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);
            BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);

            var isExist = await blockBlobClient.ExistsAsync(cancellationToken);

            if (isExist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await blockBlobClient.UploadAsync(dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new StorageException(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task<InterchangeData> ReadEntity(string path, ReadOptions options,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetParts(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);
            BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);

            var isExist = await blockBlobClient.ExistsAsync(cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var response = await blockBlobClient.OpenReadAsync(cancellationToken: cancellationToken);
            var blobProperties = await blockBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var result = new InterchangeData();
            result.Columns.Add("Content", typeof(byte[]));

            var row = result.NewRow();
            row.Metadata.ContentHash = blobProperties.Value.ContentHash?.ToHexString();
            row["Content"] = response.StreamToByteArray();

            return result;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new StorageException(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task DeleteEntity(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetParts(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);

            if (PathHelper.IsFile(path))
            {
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);
                await blockBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            var blobItems = container.GetBlobsAsync(prefix: pathParts.RelativePath);
            await foreach (BlobItem blobItem in blobItems)
            {
                BlobClient blobClient = container.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, blobItem.Name));
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new StorageException(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task PurgeEntity(string? path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var pathParts = GetParts(path);
        var directory = pathParts.RelativePath;
        var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);

        var isExist = await container.ExistsAsync(cancellationToken);
        if (!isExist)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        if (!string.IsNullOrEmpty(directory))
        {
            if (!directory.EndsWith(PathHelper.PathSeparator))
                directory += PathHelper.PathSeparator;

            BlockBlobClient blockBlobClient = container.GetBlockBlobClient(directory);
            await blockBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        else
        {
            await container.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task<bool> ExistEntity(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetParts(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);

            if (PathHelper.IsFile(path))
            {
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);
                return await blockBlobClient.ExistsAsync(cancellationToken: cancellationToken);
            }

            var blobItems = container.GetBlobsByHierarchy(prefix: pathParts.RelativePath);
            return blobItems.Select(x => x.IsPrefix).Any();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new StorageException(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesList(string path, ListOptions options,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var containers = new List<BlobContainerClient>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            containers.AddRange(await ListContainers(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(containers.Select(c => c.ToEntity(options.IncludeMetadata)));

            if (!options.Recurse)
            {
                return storageEntities;
            }
        }
        else
        {
            var pathParts = GetParts(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);

            path = pathParts.RelativePath;
            containers.Add(container);
        }

        await Task.WhenAll(containers.Select(c =>
            ListBlobs(c, storageEntities, path, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private async Task<InterchangeData> FilteredEntitiesList(string path, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var entities = await EntitiesList(path, listOptions, cancellationToken);

        var dataFilterOptions = GetDataTableOption(listOptions);
        var dataTable = entities.ListToInterchangeData();
        var filteredEntities = _dataService.Select(dataTable, dataFilterOptions);

        return (InterchangeData)filteredEntities;
    }

    private async Task<IReadOnlyCollection<BlobContainerClient>> ListContainers(CancellationToken cancellationToken)
    {
        var result = new List<BlobContainerClient>();
        BlobContainerClient logsContainerClient = _client.GetBlobContainerClient(blobContainerName: "$logs");
        Task<Response<BlobContainerProperties>> logsProps = logsContainerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        await foreach (BlobContainerItem container in _client.GetBlobContainersAsync(traits: BlobContainerTraits.Metadata).ConfigureAwait(false))
        {
            BlobContainerClient client = _client.GetBlobContainerClient(container.Name);

            if (client != null)
                result.Add(client);
        }

        try
        {
            await logsProps.ConfigureAwait(false);
            result.Add(logsContainerClient);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            _logger.LogError(string.Format(Resources.ContainerNotFound, logsContainerClient.Name));
        }

        return result;
    }

    private async Task ListBlobs(BlobContainerClient containerClient, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var containerBlobs = await ListFolder(containerClient, path, listOptions, cancellationToken).ConfigureAwait(false);

        if (containerBlobs.Count > 0)
        {
            result.AddRange(containerBlobs);
        }
    }
    
    private async Task<IReadOnlyCollection<StorageEntity>> ListFolder(BlobContainerClient containerClient, 
        string path, ListOptions listOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();

        try
        {
            var blobs = containerClient.GetBlobsByHierarchyAsync(
                delimiter: listOptions.Recurse ? null : PathHelper.PathSeparatorString,
                prefix: FormatFolderPrefix(path),
                traits: BlobTraits.Metadata,
                states: BlobStates.None
            ).ConfigureAwait(false);

            await foreach (BlobHierarchyItem item in blobs)
            {
                try
                {
                    if (item.IsBlob)
                        result.Add(item.ToEntity(containerClient.Name, listOptions.IncludeMetadata));

                    if (item.IsPrefix)
                        result.Add(item.ToEntity(containerClient.Name));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            if (listOptions.Recurse)
            {
                var implicitPrefixes = AssumeImplicitPrefixes(
                    PathHelper.Combine(containerClient.Name, path),
                    result);

                if (implicitPrefixes.Count > 0)
                {
                    result.AddRange(implicitPrefixes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return result;
    }

    private IReadOnlyCollection<StorageEntity> AssumeImplicitPrefixes(string absoluteRoot, IEnumerable<StorageEntity> blobs)
    {
        var result = new List<StorageEntity>();
        absoluteRoot = PathHelper.Normalize(absoluteRoot);

        var implicitFolders = blobs
           .Select(b => b.FullPath)
           .Select(PathHelper.GetParent)
           .Where(p => !PathHelper.IsRootPath(p))
           .Distinct()
           .Select(p => new StorageEntity(p, StorageEntityItemKind.Directory))
           .ToList();

        result.AddRange(implicitFolders);
        return result;
    }

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path, ListOptions listOptions,
    //    ReadOptions readOptions, CancellationToken cancellationToken = default)
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

    private string? FormatFolderPrefix(string folderPath)
    {
        folderPath = PathHelper.Normalize(folderPath);

        if (PathHelper.IsRootPath(folderPath))
            return null;

        if (!folderPath.EndsWith(PathHelper.PathSeparator))
            folderPath += PathHelper.PathSeparator;

        return folderPath;
    }

    private async Task<BlobContainerClient> GetBlobContainerClient(string containerName)
    {
        var container = _client.GetBlobContainerClient(containerName);

        try
        {
            await container.GetPropertiesAsync().ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            throw new StorageException(ex.Message);
        }

        return container;
    }

    private AzureBlobEntityPart GetParts(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        if (fullPath == null)
            throw new ArgumentNullException(nameof(fullPath));

        string containerName, relativePath;
        string[] parts = PathHelper.Split(fullPath);

        if (parts.Length == 1)
        {
            containerName = parts[0];
            relativePath = string.Empty;
        }
        else
        {
            containerName = parts[0];
            relativePath = PathHelper.Combine(parts.Skip(1));
        }

        return new AzureBlobEntityPart(containerName, relativePath);
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
    #endregion
}
