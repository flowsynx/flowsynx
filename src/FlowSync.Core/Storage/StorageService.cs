using EnsureThat;
using FlowSync.Abstractions.Exceptions;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Parers.Norms.Storage;
using FlowSync.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.Storage;

internal class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly IPluginsManager _pluginsManager;

    public StorageService(ILogger<StorageService> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
        _pluginsManager = pluginsManager;
    }

    public async Task<StorageUsage> About(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            storageNormsInfo.Plugin.Specifications = storageNormsInfo.Specifications;
            return await storageNormsInfo.Plugin.About(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting about information. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<IEnumerable<StorageEntity>> List(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, int? maxResult, CancellationToken cancellationToken = default)
    {
        try
        {
            storageNormsInfo.Plugin.Specifications = storageNormsInfo.Specifications;
            return await storageNormsInfo.Plugin.ListAsync(storageNormsInfo.Path, storageSearches, maxResult, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting entities list. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task Delete(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        try
        {
            storageNormsInfo.Plugin.Specifications = storageNormsInfo.Specifications;
            await storageNormsInfo.Plugin.DeleteAsync(storageNormsInfo.Path, storageSearches, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting entities list. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }

    public async Task<StorageStream> ReadAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            storageNormsInfo.Plugin.Specifications = storageNormsInfo.Specifications;
            return await storageNormsInfo.Plugin.ReadAsync(storageNormsInfo.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem read path. Message: {ex.Message}");
            throw new StorageException(ex.Message);
        }
    }
}