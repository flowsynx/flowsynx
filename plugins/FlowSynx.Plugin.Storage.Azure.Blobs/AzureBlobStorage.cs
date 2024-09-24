using Azure;
using Azure.Storage.Blobs;
using FlowSynx.Plugin.Abstractions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

public class AzureBlobStorage : PluginBase
{
    private readonly ILogger<AzureBlobStorage> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AzureBlobStorageSpecifications? _azureBlobSpecifications;
    private BlobServiceClient _client = null!;

    public AzureBlobStorage(ILogger<AzureBlobStorage> logger, IDataFilter dataFilter,
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
    public override PluginNamespace Namespace => PluginNamespace.Storage;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureBlobStorageSpecifications);

    public override Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobStorageSpecifications>();
        _client = CreateClient(_azureBlobSpecifications);
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

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

    public override async Task WriteAsync(string entity, PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

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

            if (isExist && writeOptions.Overwrite is false)
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

    public override async Task<object> ReadAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readOptions = options.ToObject<ReadOptions>();

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
            var fileExtension = Path.GetExtension(path);

            return new StorageRead()
            {
                Stream = new StorageStream(response),
                ContentType = blobProperties.Value.ContentType,
                Extension = fileExtension,
                Md5 = blobProperties.Value.ContentHash?.ToHexString(),
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

            await DeleteEntityAsync(list.Path, cancellationToken).ConfigureAwait(false);
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

    public override async Task<bool> ExistAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
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
            return blobItems.Select(x=>x.IsPrefix).Any();
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

    public override async Task<TransmissionData> PrepareTransmissionData(string entity, PluginOptions? options,
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
        var transmissionDataRows = new List<TransmissionDataRow>();

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
            transmissionDataRows.Add(new TransmissionDataRow
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
        var result = new TransmissionData
        {
            PluginNamespace = Namespace,
            PluginType = Type,
            Columns = columnNames,
            Rows = transmissionDataRows
        };

        return result;
    }

    public override async Task TransmitDataAsync(string entity, PluginOptions? options, 
        TransmissionData transmissionData, CancellationToken cancellationToken = new CancellationToken())
    {
        if (transmissionData.PluginNamespace == PluginNamespace.Storage)
        {
            foreach (var item in transmissionData.Rows)
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
            if (!string.IsNullOrEmpty(transmissionData.Content))
            {
                var fileBytes = Convert.FromBase64String(transmissionData.Content);
                await File.WriteAllBytesAsync(path, fileBytes, cancellationToken);
            }
            else
            {
                foreach (var item in transmissionData.Rows)
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

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
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
                return await blockBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }

            var blobItems = container.GetBlobsAsync(prefix: pathParts.RelativePath);
            await foreach (BlobItem blobItem in blobItems)
            {
                BlobClient blobClient = container.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }

            return true;
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

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private async Task<IEnumerable<StorageEntity>> ListEntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var containers = new List<BlobContainerClient>();

        var dataFilterOptions = GetDataFilterOptions(options);

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