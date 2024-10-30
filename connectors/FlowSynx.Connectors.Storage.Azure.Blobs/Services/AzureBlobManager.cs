using Azure;
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
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Extensions;
using System.Data;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public class AzureBlobManager : IAzureBlobManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly BlobServiceClient _client;

    public AzureBlobManager(ILogger logger, BlobServiceClient client, IDataFilter dataFilter, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _client = client;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public async Task CreateAsync(string path, CreateOptions options,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task WriteAsync(string path, WriteOptions options,
        object dataOptions, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task<ReadResult> ReadAsync(string path, ReadOptions options,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task PurgeAsync(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task<bool> ExistAsync(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
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

    public async Task<IEnumerable<StorageEntity>> EntitiesAsync(string path, ListOptions options,
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
            ListBlobsAsync(c, storageEntities, path, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    public async Task<IEnumerable<object>> FilteredEntitiesAsync(string path, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        var entities = await EntitiesAsync(path, listOptions, cancellationToken);

        var dataFilterOptions = GetFilterOptions(listOptions);
        var dataTable = entities.ToDataTable();
        var filteredEntities = _dataFilter.Filter(dataTable, dataFilterOptions);

        return filteredEntities.CreateListFromTable();
    }

    public async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path, ListOptions listOptions,
        ReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        path = PathHelper.ToUnixPath(path);

        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

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
                    var read = await ReadAsync(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
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

    public void Dispose() { }

    #region internal methods
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

    private async Task ListBlobsAsync(BlobContainerClient containerClient, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var containerBlobs = await ListFolderAsync(containerClient, path, listOptions, cancellationToken).ConfigureAwait(false);

        if (containerBlobs.Count > 0)
        {
            result.AddRange(containerBlobs);
        }
    }
    
    private async Task<IReadOnlyCollection<StorageEntity>> ListFolderAsync(BlobContainerClient containerClient, 
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

    private AzureBlobEntityPart GetPartsAsync(string fullPath)
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
