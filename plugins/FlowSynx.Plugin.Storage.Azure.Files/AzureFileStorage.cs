using Azure;
using Azure.Storage;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using Azure.Storage.Files.Shares;
using FlowSynx.Reflections;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.IO;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFileStorage : IStoragePlugin
{
    private readonly ILogger<AzureFileStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private Dictionary<string, string?>? _specifications;
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
    public Dictionary<string, string?>? Specifications
    {
        get => _specifications;
        set
        {
            _specifications = value;
            _azureFilesSpecifications = value.DictionaryToObject<AzureFilesSpecifications>();
            _client = CreateClient(_azureFilesSpecifications);
        }
    }

    public Type SpecificationsType => typeof(AzureFilesSpecifications);

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

    public async Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
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

        return new StorageUsage { Used = totalUsed };
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        var result = new List<StorageEntity>();
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
                        if (listOptions.Kind is StorageFilterItemKind.File or StorageFilterItemKind.FileAndDirectory && !item.IsDirectory)
                        {
                            result.Add(await AzureFileConverter.ToEntity(dir.Path, item, dir.GetFileClient(item.Name), cancellationToken));
                        }

                        if (listOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory && item.IsDirectory)
                        {
                            result.Add(await AzureFileConverter.ToEntity(dir.Path, item, dir, cancellationToken));
                        }

                        if (!searchOptions.Recurse) continue;

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

        var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

        if (listOptions.MaxResult is > 0)
            filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

        return filteredResult;
    }

    public async Task WriteAsync(string path, StorageStream storageStream, StorageWriteOptions writeOptions, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);

            if (isExist && writeOptions.Overwrite is false)
            {
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));
            }

            await fileClient.CreateAsync(maxSize: storageStream.Length, cancellationToken: cancellationToken);
            await fileClient.UploadRangeAsync(new HttpRange(0, storageStream.Length), storageStream, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            _logger.LogError(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidResourceName)
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

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
                Md5 = fileProperties.Value.ContentHash != null
                    ? System.Text.Encoding.UTF8.GetString(fileProperties.Value.ContentHash) 
                    : null,
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
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new StorageException(Resources.ThePathMustBeFile);
            
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            return await fileClient.ExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            return false;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(path, storageSearches,
            new StorageListOptions {Kind = StorageFilterItemKind.File}, new StorageHashOptions(), cancellationToken);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            _logger.LogWarning(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (var entity in storageEntities)
        {
            await DeleteFileAsync(entity.FullPath, cancellationToken);
        }
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotFile, path));

            await fileClient.DeleteAsync(cancellationToken: cancellationToken);
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
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await directoryClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
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
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            var isExist = await directoryClient.ExistsAsync(cancellationToken: cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedDirectoryPathIsNotDirectory, path));

            await DeleteAsync(path, new StorageSearchOptions(), cancellationToken);
            await directoryClient.DeleteAsync(cancellationToken: cancellationToken);
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
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public async Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            return await directoryClient.ExistsAsync(cancellationToken: cancellationToken);
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
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public void Dispose() { }
}