using EnsureThat;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage.Options;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage;

internal class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;

    public StorageService(ILogger<StorageService> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
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
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            return await storageNormsInfo.Plugin.ListAsync(storageNormsInfo.Path, searchOptions, listOptions, hashOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Getting entities list from storage. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task WriteAsync(StorageNormsInfo storageNormsInfo, StorageStream storageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            await storageNormsInfo.Plugin.WriteAsync(storageNormsInfo.Path, storageStream, cancellationToken);
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
            bool isFile;
            if ((isFile = PathHelper.IsFile(sourceStorageNormsInfo.Path)) != PathHelper.IsFile(destinationStorageNormsInfo.Path))
                throw new StorageException(Resources.CopyDestinationPathIsDifferentThanSourcePath);

            if (copyOptions.ClearDestinationPath is true)
            {
                _logger.LogWarning($"Purge directory from destination storage before copying.");
                await destinationStorageNormsInfo.Plugin.DeleteAsync(destinationStorageNormsInfo.Path, new StorageSearchOptions(), cancellationToken);
            }

            if (isFile)
                CopyFile(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, 
                    destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, 
                    copyOptions.OverWriteData, cancellationToken);
            else
                CopyDirectory(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, 
                    destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, 
                    searchOptions, copyOptions.OverWriteData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Copy from storage '{sourceStorageNormsInfo.Plugin.Name}' to {destinationStorageNormsInfo.Plugin.Name}. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    private async void CopyFile(IStoragePlugin sourcePlugin, string sourceFile, IStoragePlugin destinationPlugin, 
        string destinationFile, bool? overWrite, CancellationToken cancellationToken = default)
    {
        var fileExist = await sourcePlugin.FileExistAsync(destinationFile, cancellationToken);
        if (overWrite is null or false && fileExist)
        {
            _logger.LogInformation($"Copy operation ignored - The file '{destinationFile}' is already exist on '{destinationPlugin.Name}'");
            return;
        }

        var sourceStream = await sourcePlugin.ReadAsync(sourceFile, new StorageHashOptions(), cancellationToken);
        await destinationPlugin.WriteAsync(destinationFile, sourceStream.Stream, cancellationToken);
        _logger.LogInformation($"Copy operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for file '{sourceFile}'");
    }

    private async void CopyDirectory(IStoragePlugin sourcePlugin, string sourceDirectory, 
        IStoragePlugin destinationPlugin, string destinationDirectory, 
        StorageSearchOptions searchOptions, bool? overWrite, CancellationToken cancellationToken = default)
    {
        if (!PathHelper.IsRootPath(destinationDirectory))
            await destinationPlugin.MakeDirectoryAsync(destinationDirectory, cancellationToken);

        var entities = await sourcePlugin.ListAsync(sourceDirectory, searchOptions, 
            new StorageListOptions(), new StorageHashOptions(), cancellationToken);

        var storageEntities = entities.ToList();
        foreach (string dirPath in storageEntities.Where(x => x.Kind == StorageEntityItemKind.Directory))
        {
            var destinationDir = dirPath.Replace(sourceDirectory, destinationDirectory);
            await destinationPlugin.MakeDirectoryAsync(destinationDir, cancellationToken);
            _logger.LogInformation($"Copy operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for directory '{dirPath}'");
        }

        foreach (string file in storageEntities.Where(x => x.Kind == StorageEntityItemKind.File))
        {
            var destinationFile = file.Replace(sourceDirectory, destinationDirectory);
            CopyFile(sourcePlugin, file, destinationPlugin, destinationFile, overWrite, cancellationToken);
        }
    }

    public async Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, StorageMoveOptions moveOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.Equals(sourceStorageNormsInfo.Plugin.Name, destinationStorageNormsInfo.Plugin.Name, StringComparison.InvariantCultureIgnoreCase) && 
                string.Equals(sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Path, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new StorageException(Resources.MoveTheSourceAndDestinationPathAreIdenticalAndOverlap);
            }

            bool isFile;
            if ((isFile = PathHelper.IsFile(sourceStorageNormsInfo.Path)) != PathHelper.IsFile(destinationStorageNormsInfo.Path))
                throw new StorageException(Resources.MoveDestinationPathIsDifferentThanSourcePath);

            if (isFile)
                MoveFile(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, cancellationToken);
            else
                MoveDirectory(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, searchOptions, cancellationToken);

            if (!PathHelper.IsRootPath(sourceStorageNormsInfo.Path))
                await sourceStorageNormsInfo.Plugin.DeleteAsync(sourceStorageNormsInfo.Path, searchOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Move from storage '{sourceStorageNormsInfo.Plugin.Name}' to {destinationStorageNormsInfo.Plugin.Name}. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    private async void MoveFile(IStoragePlugin sourcePlugin, string sourceFile, IStoragePlugin destinationPlugin, string destinationFile, CancellationToken cancellationToken = default)
    {
        var sourceStream = await sourcePlugin.ReadAsync(sourceFile, new StorageHashOptions(), cancellationToken);
        await destinationPlugin.WriteAsync(destinationFile, sourceStream.Stream, cancellationToken);
        _logger.LogInformation($"Move operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for file '{sourceFile}'");
    }

    private async void MoveDirectory(IStoragePlugin sourcePlugin, string sourceDirectory, IStoragePlugin destinationPlugin, string destinationDirectory, StorageSearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        if (!PathHelper.IsRootPath(destinationDirectory))
            await destinationPlugin.MakeDirectoryAsync(destinationDirectory, cancellationToken);

        var entities = await sourcePlugin.ListAsync(sourceDirectory, searchOptions,
            new StorageListOptions(), new StorageHashOptions(), cancellationToken);

        var storageEntities = entities.ToList();
        foreach (string dirPath in storageEntities.Where(x => x.Kind == StorageEntityItemKind.Directory))
        {
            var destinationDir = dirPath.Replace(sourceDirectory, destinationDirectory);
            await destinationPlugin.MakeDirectoryAsync(destinationDir, cancellationToken);
            _logger.LogInformation($"Copy operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for directory '{dirPath}'");
        }

        foreach (string file in storageEntities.Where(x => x.Kind == StorageEntityItemKind.File))
        {
            var destinationFile = file.Replace(sourceDirectory, destinationDirectory);
            MoveFile(sourcePlugin, file, destinationPlugin, destinationFile, cancellationToken);
        }
    }
}