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
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

public class AzureBlobStorage : IPlugin
{
    private readonly ILogger<AzureBlobStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private AzureBlobStorageSpecifications? _azureBlobSpecifications;
    private BlobServiceClient _client = null!;

    public AzureBlobStorage(ILogger<AzureBlobStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public string Name => "Azure.Blobs";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(AzureBlobStorageSpecifications);

    public Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobStorageSpecifications>();
        _client = CreateClient(_azureBlobSpecifications);
        return Task.CompletedTask;
    }

    public Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public async Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createFilters = filters.ToObject<CreateFilters>();

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

            var result = new StorageEntity(path, StorageEntityItemKind.Directory);
            return new { result.Id };
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

    public async Task<object> WriteAsync(string entity, PluginFilters? filters, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeFilters = filters.ToObject<WriteFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (dataOptions is not Stream dataStream)
            throw new StorageException(nameof(dataStream));

        try
        {
            var pathParts = GetPartsAsync(path);
            var container = await GetBlobContainerClient(pathParts.ContainerName).ConfigureAwait(false);
            BlockBlobClient blockBlobClient = container.GetBlockBlobClient(pathParts.RelativePath);

            var isExist = await blockBlobClient.ExistsAsync(cancellationToken);

            if (isExist && writeFilters.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await blockBlobClient.UploadAsync(dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var result = new StorageEntity(path, StorageEntityItemKind.File);
            return new { result.Id };
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

    public async Task<object> ReadAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readFilters = filters.ToObject<ReadFilters>();

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

    public Task<object> UpdateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<object>> DeleteAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteFilters = filters.ToObject<DeleteFilters>();
        var entities = await ListAsync(path, filters, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var result = new List<string>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list)
                continue;

            if (await DeleteEntityAsync(list.Path, cancellationToken).ConfigureAwait(false))
            {
                result.Add(list.Id);
            }
        }

        if (deleteFilters.Purge is true)
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

        return result;
    }

    public async Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
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

    public async Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var containers = new List<BlobContainerClient>();
        var listFilters = filters.ToObject<ListFilters>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            // list all of the containers
            containers.AddRange(await ListContainersAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(containers.Select(c => c.ToEntity(listFilters.IncludeMetadata)));

            if (!listFilters.Recurse)
            {
                var containerEntities = new List<StorageList>(storageEntities.Count());
                containerEntities.AddRange(storageEntities.Select(storageEntity => new StorageList
                {
                    Id = storageEntity.Id,
                    Kind = storageEntity.Kind.ToString().ToLower(),
                    Name = storageEntity.Name,
                    Path = storageEntity.FullPath,
                    CreatedTime = storageEntity.CreatedTime,
                    ModifiedTime = storageEntity.ModifiedTime,
                    Size = storageEntity.Size.ToString(!listFilters.Full),
                    ContentType = storageEntity.ContentType,
                    Md5 = storageEntity.Md5,
                    Metadata = storageEntity.Metadata
                }));

                return containerEntities;
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
            ListAsync(c, storageEntities, path, listFilters, cancellationToken))
        ).ConfigureAwait(false);

        var filteredEntities = _storageFilter.Filter(storageEntities, filters).ToList();

        var result = new List<StorageList>(filteredEntities.Count());
        result.AddRange(filteredEntities.Select(storageEntity => new StorageList
        {
            Id = storageEntity.Id,
            Kind = storageEntity.Kind.ToString().ToLower(),
            Name = storageEntity.Name,
            Path = storageEntity.FullPath,
            CreatedTime = storageEntity.CreatedTime,
            ModifiedTime = storageEntity.ModifiedTime,
            Size = storageEntity.Size.ToString(!listFilters.Full),
            ContentType = storageEntity.ContentType,
            Md5 = storageEntity.Md5,
            Metadata = storageEntity.Metadata
        }));

        return result;
    }

    public async Task<object> CompressAsync(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, filters, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList entry)
            {
                _logger.LogWarning("The item is not valid object type. It should be StorageEntity type.");
                continue;
            }

            if (!string.Equals(entry.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entry.Name}' is not a file.");
                continue;
            }

            var stream = await ReadAsync(entry.Path, filters, cancellationToken);
            if (stream is not StorageRead storageRead)
            {
                _logger.LogWarning($"The item '{entry.Name}' could be not read.");
                continue;
            }

            compressEntries.Add(new CompressEntry
            {
                Name = entry.Name,
                ContentType = entry.ContentType,
                Stream = storageRead.Stream
            });
        }

        return compressEntries;
    }

    public void Dispose() { }

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
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        using var browser = new AzureContainerBrowser(_logger, containerClient);
        IReadOnlyCollection<StorageEntity> containerBlobs =
            await browser.ListFolderAsync(path, listFilters, cancellationToken).ConfigureAwait(false);

        if (containerBlobs.Count > 0)
        {
            result.AddRange(containerBlobs);
        }
    }

    private async Task<IReadOnlyCollection<BlobContainerClient>> ListContainersAsync(CancellationToken cancellationToken)
    {
        var result = new List<BlobContainerClient>();

        //check that the special "$logs" container exists
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
    #endregion
}