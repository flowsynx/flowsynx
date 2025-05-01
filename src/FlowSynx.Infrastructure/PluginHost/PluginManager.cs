using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginRegistryConfiguration _pluginRegistryConfiguration;
    private readonly IPluginsLocation _pluginsLocation;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginDownloader _pluginDownloader;
    //private readonly IPluginLoader _pluginLoader;
    private readonly IPluginCacheService _pluginCacheService;

    public PluginManager(
        ILogger<PluginManager> logger,
        PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation, 
        ICurrentUserService currentUserService, 
        IPluginService pluginService,
        IPluginDownloader pluginDownloader, 
        //IPluginLoader pluginLoader,
        IPluginCacheService pluginCacheService)
    {
        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration;
        _pluginsLocation = pluginsLocation;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginDownloader = pluginDownloader;
        //_pluginLoader = pluginLoader;
        _pluginCacheService = pluginCacheService;
    }

    public async Task InstallAsync(string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        if (await PluginAlreadyExists(pluginType, pluginVersion, cancellationToken))
            return;

        var pluginMetadata = await DownloadPluginMetadata(pluginType, pluginVersion);
        var pluginData = await _pluginDownloader.GetPluginDataAsync(pluginMetadata.Url);

        if (!ValidatePluginChecksum(pluginData, pluginMetadata.Checksum))
            return;

        string pluginDirectory = GetPluginLocalDirectory(pluginType, pluginVersion);
        await _pluginDownloader.ExtractPluginAsync(pluginDirectory, pluginData, cancellationToken);

        int installedCount = await InstallPluginAssemblies(pluginDirectory, pluginMetadata, cancellationToken);

        if (installedCount == 0)
            throw new FlowSynxException((int)ErrorCode.PluginInstallationNotFound, 
                "No plugin was installed from the package.");
    }

    public async Task UpdateAsync(string pluginType, string oldVersion, string newPluginVersion, CancellationToken cancellationToken)
    {
        await Uninstall(pluginType, oldVersion, cancellationToken);
        await InstallAsync(pluginType, newPluginVersion, cancellationToken);
    }

    public async Task Uninstall(string pluginType, string version, CancellationToken cancellationToken)
    {
        var pluginEntity = await _pluginService.Get(_currentUserService.UserId, pluginType, version, cancellationToken);
        if (pluginEntity is null)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginNotFound,
                    string.Format(Resources.PluginManager_PluginCouldNotFound, pluginType, version));
            throw new FlowSynxException(errorMessage);
        }

        try
        {
            var index = new PluginCacheIndex(_currentUserService.UserId, pluginEntity.Type, pluginEntity.Version);
            _pluginCacheService.RemoveByIndex(index);

            var parentLocation = Directory.GetParent(pluginEntity.PluginLocation);
            if (parentLocation != null)
            {
                if (parentLocation.Exists)
                {
                    RemoveReadOnlyAttribute(parentLocation);
                    parentLocation.Delete(true);
                }
            }

            await _pluginService.Delete(pluginEntity, cancellationToken);
            _logger.LogInformation(string.Format(Resources.PluginManager_PluginUninstalledSuccessfully, pluginType, version));
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.PluginUninstall, ex.Message);
        }
    }

    #region internal methods
    private async Task<bool> PluginAlreadyExists(string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        var exists = await _pluginService.IsExist(_currentUserService.UserId, pluginType, pluginVersion, cancellationToken);
        if (!exists)
            return false;

        var errorMessage = new ErrorMessage(
            (int)ErrorCode.PluginCheckExistence,
            string.Format(Resources.PluginManager_Install_PluginIsAlreadyExist, pluginType, pluginVersion)
        );

        throw new FlowSynxException(errorMessage);
    }

    private async Task<PluginInstallMetadata> DownloadPluginMetadata(string pluginType, string pluginVersion)
    {
        var registryUrl = _pluginRegistryConfiguration.Url;
        return await _pluginDownloader.GetPluginMetadataAsync(registryUrl, pluginType, pluginVersion);
    }

    private bool ValidatePluginChecksum(byte[] pluginData, string checksum)
    {
        if (_pluginDownloader.ValidateChecksum(pluginData, checksum))
            return true;

        _logger.LogError(Resources.PluginManager_Install_ChecksumValidationFailed);
        return false;
    }

    private string GetPluginLocalDirectory(string pluginType, string pluginVersion)
    {
        var rootLocation = Path.Combine(_pluginsLocation.Path, _currentUserService.UserId);
        return Path.Combine(rootLocation, pluginType, pluginVersion);
    }

    private async Task<int> InstallPluginAssemblies(
        string pluginDirectory,
        PluginInstallMetadata metadata,
        CancellationToken cancellationToken)
    {
        int count = 0;

        foreach (var dllPath in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            PluginLoader? pluginLoader = null;
            try
            {
                pluginLoader = new PluginLoader(dllPath);
                var pluginEntity = CreatePluginEntity(metadata, dllPath, pluginLoader);
                await _pluginService.Add(pluginEntity, cancellationToken);
                _logger.LogInformation(string.Format(Resources.PluginManager_Install_PluginInstalledSuccessfully,
                    metadata.Type, metadata.Version));

                count++;
                
            }
            catch (Exception ex)
            {
                _logger.LogDebug(string.Format(Resources.PluginManager_Install_ErrorLoading, ex.Message));
                continue;
            }
            finally
            {
                pluginLoader?.Unload();
            }
        }

        return count;
    }

    private PluginEntity CreatePluginEntity(
        PluginInstallMetadata metadata,
        string dllPath,
        PluginLoader handle)
    {
        return new PluginEntity
        {
            Id = Guid.NewGuid(),
            PluginId = metadata.Id,
            Name = metadata.Name,
            Version = metadata.Version,
            Checksum = metadata.Checksum,
            PluginLocation = dllPath,
            UserId = _currentUserService.UserId,
            Author = metadata.Author,
            Description = metadata.Description,
            Type = metadata.Type,
            Specifications = handle.Plugin.GetPluginSpecification()
        };
    }

    private void RemoveReadOnlyAttribute(DirectoryInfo directoryInfo)
    {
        foreach (var fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            if (fileInfo.IsReadOnly)
                fileInfo.IsReadOnly = false;
        }
    }
    #endregion
}