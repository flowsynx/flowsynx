using Azure;
using Azure.Storage;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.Connectors.Storage.Exceptions;

namespace FlowSynx.Connectors.Storage.Azure.Files;

public class AzureFileConnector : Connector
{
    private readonly ILogger<AzureFileConnector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AzureFilesSpecifications? _azureFilesSpecifications;
    private ShareClient _client = null!;

    public AzureFileConnector(ILogger<AzureFileConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d");
    public override string Name => "Azure.Files";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureFilesSpecifications);

    public override Task Initialize()
    {
        _azureFilesSpecifications = Specifications.ToObject<AzureFilesSpecifications>();
        _client = CreateClient(_azureFilesSpecifications);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        long totalUsed;
        try
        {
            var state = await _client.GetStatisticsAsync(cancellationToken);
            totalUsed = state.Value.ShareUsageInBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalUsed = 0;
        }

        return new
        {
            Total = totalUsed
        };
    }

    public override async Task CreateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var createOptions = options.ToObject<CreateOptions>();
        await CreateEntityAsync(context.Entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options, 
        object dataOptions, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var writeOptions = options.ToObject<WriteOptions>();
        await WriteEntityAsync(context.Entity, writeOptions, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var readOptions = options.ToObject<ReadOptions>();
        return await ReadEntityAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var deleteOptions = options.ToObject<DeleteOptions>();
        var dataTable = await FilteredEntitiesAsync(path, listOptions, cancellationToken).ConfigureAwait(false);
        var entities = dataTable.CreateListFromTable();

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageEntity storageEntity)
                continue;

            await DeleteEntityAsync(storageEntity.FullPath, cancellationToken).ConfigureAwait(false);
        }

        if (deleteOptions.Purge is true)
        {
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await directoryClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        if (string.IsNullOrWhiteSpace(path))
            throw new StorageException(Resources.ThePathMustBeFile);

        try
        {
            if (PathHelper.IsDirectory(path))
            {
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
                return await directoryClient.ExistsAsync(cancellationToken: cancellationToken);
            }

            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            return await fileClient.ExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var filteredData = await FilteredEntitiesAsync(context.Entity, listOptions, cancellationToken).ConfigureAwait(false);
        return filteredData.CreateListFromTable();
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        if (destinationConnector is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var readOptions = options.ToObject<ReadOptions>();

        var transferData = await PrepareTransferring(sourceContext, listOptions, readOptions, cancellationToken);

        foreach (var row in transferData.Rows)
            row.Key = row.Key.Replace(sourceContext.Entity, destinationContext.Entity);

        await destinationConnector.ProcessTransferAsync(destinationContext, transferData, options, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        var createOptions = options.ToObject<CreateOptions>();
        var writeOptions = options.ToObject<WriteOptions>();

        var path = PathHelper.ToUnixPath(context.Entity);

        if (!string.IsNullOrEmpty(transferData.Content))
        {
            var parentPath = PathHelper.GetParent(path);
            if (!PathHelper.IsRootPath(parentPath))
            {
                await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                await WriteEntityAsync(path, writeOptions, transferData.Content, cancellationToken).ConfigureAwait(false);
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
                        await CreateEntityAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                        await WriteEntityAsync(item.Key, writeOptions, item.Content, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

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
            }
        }

        return compressEntries;
    }

    #region internal methods
    private ShareClient CreateClient(AzureFilesSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.ShareName))
            throw new StorageException(Resources.ShareNameInSpecificationShouldBeNotEmpty);

        if (!string.IsNullOrEmpty(specifications.ConnectionString))
            return new ShareClient(specifications.ConnectionString, specifications.ShareName);

        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new StorageException(Resources.OnePropertyShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.file.core.windows.net/{specifications.ShareName}");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new ShareClient(shareUri: uri, credential: credential);
    }

    private async Task DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
                await fileClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await DeleteAllAsync(directoryClient, cancellationToken: cancellationToken);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    private async Task DeleteAllAsync(ShareDirectoryClient dirClient, CancellationToken cancellationToken)
    {

        await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory)
            {
                var subDir = dirClient.GetSubdirectoryClient(item.Name);
                await DeleteAllAsync(subDir, cancellationToken: cancellationToken);
            }
            else
            {
                await dirClient.DeleteFileAsync(item.Name, cancellationToken: cancellationToken);
            }
        }

        await dirClient.DeleteAsync(cancellationToken);
    }

    private async Task CreateEntityAsync(string entity, CreateOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        try
        {
            var pathParts = PathHelper.Split(path);
            string proceedPath = string.Empty;
            foreach (var part in pathParts)
            {
                proceedPath = PathHelper.Combine(proceedPath, part);
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(proceedPath);
                await directoryClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    private async Task WriteEntityAsync(string entity, WriteOptions options, 
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

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (isExist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var createOption = new CreateOptions { Hidden = false };
            var parentPath = PathHelper.GetParent(path) + PathHelper.PathSeparatorString;
            await CreateEntityAsync(parentPath, createOption, cancellationToken);

            await fileClient.CreateAsync(maxSize: dataStream.Length, cancellationToken: cancellationToken);
            await fileClient.UploadRangeAsync(new HttpRange(0, dataStream.Length), dataStream, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidResourceName)
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    private async Task<ReadResult> ReadEntityAsync(string entity, ReadOptions options, 
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var stream = await fileClient.OpenReadAsync(cancellationToken: cancellationToken);
            var fileProperties = await fileClient.GetPropertiesAsync(cancellationToken);

            return new ReadResult
            {
                Content = stream.StreamToByteArray(),
                ContentHash = fileProperties.Value.ContentHash?.ToHexString(),
            };
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
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
        ShareDirectoryClient directoryClient;

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
            directoryClient = _client.GetRootDirectoryClient();
        else
            directoryClient = _client.GetDirectoryClient(path);

        var remaining = new Queue<ShareDirectoryClient>();
        remaining.Enqueue(directoryClient);
        while (remaining.Count > 0)
        {
            ShareDirectoryClient dir = remaining.Dequeue();
            try
            {
                await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
                {
                    try
                    {
                        if (item.IsDirectory)
                            storageEntities.Add(await dir.ToEntity(item, options.IncludeMetadata,
                                cancellationToken));
                        else
                            storageEntities.Add(await dir.ToEntity(item, dir.GetFileClient(item.Name),
                                options.IncludeMetadata, cancellationToken));

                        if (!options.Recurse) continue;

                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                    }
                    catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
                    {
                        _logger.LogError(string.Format(Resources.ShareItemNotFound, item.Name));
                    }
                }
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
            {
                throw new StorageException(string.Format(Resources.ResourceNotExist, dir.Name));
            }
        }

        return storageEntities;
    }

    private async Task<TransferData> PrepareTransferring(Context context, ListOptions listOptions, 
        ReadOptions readOptions, CancellationToken cancellationToken)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);

        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

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
                    var read = await ReadEntityAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
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
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRows
        };

        return result;
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