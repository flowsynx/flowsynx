using Azure;
using Azure.Storage.Blobs;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.Connectors.Storage.Exceptions;

namespace FlowSynx.Connectors.Storage.Azure.Blobs;

public class AzureBlobConnector : Connector
{
    private readonly ILogger<AzureBlobConnector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AzureBlobStorageSpecifications? _azureBlobSpecifications;
    private BlobServiceClient _client = null!;

    public AzureBlobConnector(ILogger<AzureBlobConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public override string Name => "Azure.Blobs";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureBlobStorageSpecifications);

    public override Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobStorageSpecifications>();
        _client = CreateClient(_azureBlobSpecifications);
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        throw new StorageException(Resources.AboutOperrationNotSupported);
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
            var pathParts = GetPartsAsync(path);
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
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        return await ExistEntityAsync(context.Entity, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var filteredData = await FilteredEntitiesAsync(context.Entity, listOptions, cancellationToken);
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

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
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
                var stream = await ReadEntityAsync(entityItem.FullPath, readOptions, cancellationToken);
                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
                    Content = stream.Content,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return compressEntries;
    }

    #region private methods
    private BlobServiceClient CreateClient(AzureBlobStorageSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new StorageException(Resources.PropertiesShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.blob.core.windows.net");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new BlobServiceClient(serviceUri: uri, credential: credential);
    }

    private AzureContainerPathPart GetPartsAsync(string fullPath)
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

        return new AzureContainerPathPart(containerName, relativePath);
    }

    private async Task ListAsync(BlobContainerClient containerClient, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        using var browser = new AzureContainerBrowser(_logger, containerClient);
        IReadOnlyCollection<StorageEntity> containerBlobs =
            await browser.ListFolderAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

        if (containerBlobs.Count > 0)
        {
            result.AddRange(containerBlobs);
        }
    }

    private async Task<IReadOnlyCollection<BlobContainerClient>> ListContainersAsync(CancellationToken cancellationToken)
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
            var pathParts = GetPartsAsync(path);
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
            var pathParts = GetPartsAsync(path);
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
            var pathParts = GetPartsAsync(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);
            BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);

            var isExist = await blockBlobClient.ExistsAsync(cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var response = await blockBlobClient.OpenReadAsync(cancellationToken: cancellationToken);
            var blobProperties = await blockBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return new ReadResult
            {
                Content = response.StreamToByteArray(),
                ContentHash = blobProperties.Value.ContentHash?.ToHexString(),
            };
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

    private async Task DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
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

    private async Task<bool> ExistEntityAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
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
        var containers = new List<BlobContainerClient>();
        
        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            containers.AddRange(await ListContainersAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(containers.Select(c => c.ToEntity(options.IncludeMetadata)));

            if (!options.Recurse)
            {
                return storageEntities;
            }
        }
        else
        {
            var pathParts = GetPartsAsync(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);

            path = pathParts.RelativePath;
            containers.Add(container);
        }

        await Task.WhenAll(containers.Select(c =>
            ListAsync(c, storageEntities, path, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private async Task<TransferData> PrepareTransferring(Context context, ListOptions listOptions, 
        ReadOptions readOptions, CancellationToken cancellationToken)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);

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
                    var read = await ReadEntityAsync(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
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