using EnsureThat;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;
using FlowSynx.Core.Storage.Copy;
using FlowSynx.Core.Storage.Move;
using FlowSynx.Core.Storage.Check;
using FlowSynx.Core.Storage.Compress;

namespace FlowSynx.Core.Storage;

internal class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly IEntityCopier _entityCopier;
    private readonly IEntityMover _entityMover;
    private readonly IEntityChecker _entityChecker;
    private readonly IEntityCompress _entityCompress;

    public StorageService(ILogger<StorageService> logger,
        IEntityCopier entityCopier, IEntityMover entityMover, 
        IEntityChecker entityChecker, IEntityCompress entityCompress)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(entityCopier, nameof(entityCopier));
        EnsureArg.IsNotNull(entityMover, nameof(entityMover));
        EnsureArg.IsNotNull(entityChecker, nameof(entityChecker));
        EnsureArg.IsNotNull(entityCompress, nameof(entityCompress));
        _logger = logger;
        _entityCopier = entityCopier;
        _entityMover = entityMover;
        _entityChecker = entityChecker;
        _entityCompress = entityCompress;
    }

    public async Task<StorageUsage> About(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            return await storageNormsInfo.Plugin.About(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Getting information about a storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<IEnumerable<StorageEntity>> List(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await storageNormsInfo.Plugin.ListAsync(storageNormsInfo.Path, searchOptions, 
                listOptions, hashOptions, metadataOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Getting entities list from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task WriteAsync(StorageNormsInfo storageNormsInfo, StorageStream storageStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.WriteAsync(storageNormsInfo.Path, storageStream, writeOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Read file. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<StorageRead> ReadAsync(StorageNormsInfo storageNormsInfo, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await storageNormsInfo.Plugin.ReadAsync(storageNormsInfo.Path, hashOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Read file. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task Delete(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.DeleteAsync(storageNormsInfo.Path, storageSearches, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Delete files from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task DeleteFile(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.DeleteFileAsync(storageNormsInfo.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Delete file from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<bool> FileExist(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            return await storageNormsInfo.Plugin.FileExistAsync(storageNormsInfo.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"File exist in storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task MakeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.MakeDirectoryAsync(storageNormsInfo.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Make directory from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task PurgeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.PurgeDirectoryAsync(storageNormsInfo.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Purge directory from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task Copy(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo,
        StorageSearchOptions searchOptions, StorageCopyOptions copyOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            await _entityCopier.Copy(sourceStorageNormsInfo, destinationStorageNormsInfo, searchOptions, 
                copyOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Copy from storage '{sourceStorageNormsInfo.Plugin.Name}' to {destinationStorageNormsInfo.Plugin.Name}. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }
    
    public async Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, 
        StorageSearchOptions searchOptions, StorageMoveOptions moveOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            await _entityMover.Move(sourceStorageNormsInfo, destinationStorageNormsInfo, searchOptions, 
                moveOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Move from storage '{sourceStorageNormsInfo.Plugin.Name}' to {destinationStorageNormsInfo.Plugin.Name}. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }
    
    public async Task<IEnumerable<CheckResult>> Check(StorageNormsInfo sourceStorageNormsInfo, 
        StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, 
        StorageCheckOptions checkOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _entityChecker.Check(sourceStorageNormsInfo, destinationStorageNormsInfo, 
                searchOptions, checkOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Move from storage '{sourceStorageNormsInfo.Plugin.Name}' to {destinationStorageNormsInfo.Plugin.Name}. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<CompressResult> Compress(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageCompressionOptions compressionOptions, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _entityCompress.Compress(storageNormsInfo, searchOptions, listOptions, hashOptions, 
                compressionOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Getting entities list from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }
}