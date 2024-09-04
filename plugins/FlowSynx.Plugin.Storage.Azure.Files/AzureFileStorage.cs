using Azure;
using Azure.Storage;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFileStorage : IPlugin
{
    private readonly ILogger<AzureFileStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private AzureFilesSpecifications? _azureFilesSpecifications;
    private ShareClient _client = null!;

    public AzureFileStorage(ILogger<AzureFileStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
    }
    
    public Guid Id => Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d");
    public string Name => "Azure.Files";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(AzureFilesSpecifications);

    public Task Initialize()
    {
        _azureFilesSpecifications = Specifications.ToObject<AzureFilesSpecifications>();
        _client = CreateClient(_azureFilesSpecifications);
        return Task.CompletedTask;
    }

    public async Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        long totalUsed;
        var aboutFilters = filters.ToObject<AboutFilters>();

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
            Total = totalUsed.ToString(!aboutFilters.Full)
        };
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
            var pathParts = PathHelper.Split(path);
            string proceedPath = string.Empty;
            foreach (var part in pathParts)
            {
                proceedPath = PathHelper.Combine(proceedPath, part);
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(proceedPath);
                await directoryClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
            
            var result = new StorageEntity(path, StorageEntityItemKind.Directory);
            return new { result.Id };
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
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (isExist && writeFilters.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var parentPath = PathHelper.GetParent(path) + PathHelper.PathSeparatorString;
            await CreateAsync(parentPath, null, cancellationToken);

            await fileClient.CreateAsync(maxSize: dataStream.Length, cancellationToken: cancellationToken);
            await fileClient.UploadRangeAsync(new HttpRange(0, dataStream.Length), dataStream, cancellationToken: cancellationToken);

            var result = new StorageEntity(path, StorageEntityItemKind.File);
            return new { result.Id };
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
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var stream = await fileClient.OpenReadAsync(cancellationToken: cancellationToken);
            var fileProperties = await fileClient.GetPropertiesAsync(cancellationToken);
            var fileExtension = Path.GetExtension(path);

            return new StorageRead()
            {
                Stream = new StorageStream(stream),
                ContentType = fileProperties.Value.ContentType,
                Extension = fileExtension,
                Md5 = fileProperties.Value.ContentHash?.ToHexString(),
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
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await directoryClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        return result;
    }

    public async Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
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

    public async Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        ShareDirectoryClient directoryClient;
        var listFilters = filters.ToObject<ListFilters>();

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
                            storageEntities.Add(await dir.ToEntity(item, listFilters.IncludeMetadata,
                                cancellationToken));
                        else
                            storageEntities.Add(await dir.ToEntity(item, dir.GetFileClient(item.Name),
                                listFilters.IncludeMetadata, cancellationToken));

                        if (!listFilters.Recurse) continue;

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

    public async Task<IEnumerable<TransmissionData>> PrepareTransmissionData(string entity, PluginFilters? filters,
            CancellationToken cancellationToken = new CancellationToken())
    {
        if (PathHelper.IsFile(entity))
        {
            var copyFile = await PrepareCopyFile(entity, cancellationToken);
            return new List<TransmissionData>() { copyFile };
        }

        return await PrepareCopyDirectory(entity, filters, cancellationToken);
    }

    private async Task<TransmissionData> PrepareCopyFile(string entity, CancellationToken cancellationToken = default)
    {
        var sourceStream = await ReadAsync(entity, null, cancellationToken);

        if (sourceStream is not StorageRead storageRead)
            throw new StorageException($"Copy operation for file '{entity} could not proceed!'");

        return new TransmissionData(entity, storageRead.Stream, storageRead.ContentType);
    }

    private async Task<IEnumerable<TransmissionData>> PrepareCopyDirectory(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(entity, filters, cancellationToken).ConfigureAwait(false);
        var storageEntities = entities.ToList().ConvertAll(item => (StorageList)item);

        var result = new List<TransmissionData>(storageEntities.Count);

        foreach (var entityItem in storageEntities)
        {
            TransmissionData transmissionData;
            if (string.Equals(entityItem.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                var read = await ReadAsync(entityItem.Path, null, cancellationToken);
                if (read is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entityItem.Name}' could be not read.");
                    continue;
                }
                transmissionData = new TransmissionData(entityItem.Path, storageRead.Stream, storageRead.ContentType);
            }
            else
            {
                transmissionData = new TransmissionData(entityItem.Path);
            }

            result.Add(transmissionData);
        }

        return result;
    }

    public async Task<IEnumerable<object>> TransmitDataAsync(string entity, PluginFilters? filters, IEnumerable<TransmissionData> transmissionData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = new List<object>();
        var data = transmissionData.ToList();
        foreach (var item in data)
        {
            switch (item.Content)
            {
                case null:
                    result.Add(await CreateAsync(item.Key, filters, cancellationToken));
                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    break;
                case StorageStream stream:
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateAsync(parentPath, filters, cancellationToken);
                        result.Add(await WriteAsync(item.Key, filters, stream, cancellationToken));
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                    break;
            }
        }

        return result;
    }
    
    public async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginFilters? filters,
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

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
                return await fileClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }

            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await DeleteAllAsync(directoryClient, cancellationToken: cancellationToken);
            return true;
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
    #endregion

    public void Dispose() { }
}