using FlowSynx.Application.Configuration;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Infrastructure.PluginHost.PluginLoaders;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.PluginHost.Manager;

public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginRegistryConfiguration _pluginRegistryConfiguration;
    private readonly IPluginsLocation _pluginsLocation;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginDownloader _pluginDownloader;
    private readonly IPluginCacheService _pluginCacheService;
    private readonly ILocalization _localization;
    private const string PluginSearchPattern = "*.dll";

    public PluginManager(
        ILogger<PluginManager> logger,
        PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation,
        ICurrentUserService currentUserService,
        IPluginService pluginService,
        IPluginDownloader pluginDownloader,
        IPluginCacheService pluginCacheService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginRegistryConfiguration);
        ArgumentNullException.ThrowIfNull(pluginsLocation);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(pluginDownloader);
        ArgumentNullException.ThrowIfNull(pluginCacheService);
        ArgumentNullException.ThrowIfNull(localization);

        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration;
        _pluginsLocation = pluginsLocation;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginDownloader = pluginDownloader;
        _pluginCacheService = pluginCacheService;
        _localization = localization;
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
                _localization.Get("Plugin_Install_NoPluginInstalled"));
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
                    _localization.Get("PluginManager_PluginCouldNotFound", pluginType, version));
            throw new FlowSynxException(errorMessage);
        }

        try
        {
            var index = new PluginCacheIndex(_currentUserService.UserId, pluginEntity.Type, pluginEntity.Version);
            _pluginCacheService.RemoveByIndex(index);
            await Task.Delay(1000, cancellationToken);

            var parentLocation = Directory.GetParent(pluginEntity.PluginLocation);
            if (parentLocation is { Exists: true })
            {
                RemoveReadOnlyAttribute(parentLocation);
                parentLocation.Delete(true);
            }

            await _pluginService.Delete(pluginEntity, cancellationToken);
            _logger.LogInformation(_localization.Get("PluginManager_PluginUninstalledSuccessfully", pluginType, version));
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
            _localization.Get("PluginManager_Install_PluginIsAlreadyInstalled", pluginType, pluginVersion)
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

        _logger.LogError(_localization.Get("PluginManager_Install_ChecksumValidationFailed"));
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
        var count = 0;

        foreach (var dllPath in Directory.GetFiles(pluginDirectory, PluginSearchPattern))
        {
            var loader = new TransientPluginLoader(dllPath);
            try
            {
                loader.Load();
                var pluginEntity = CreatePluginEntity(metadata, dllPath, loader.Plugin);
                await _pluginService.Add(pluginEntity, cancellationToken);
                _logger.LogInformation(_localization.Get("PluginManager_Install_PluginInstalledSuccessfully",
                    metadata.Type, metadata.Version));

                count++;
            }
            catch (FlowSynxException ex)
            {
                _logger.LogDebug(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(_localization.Get("PluginManager_Install_ErrorLoading", ex.Message));
            }
            finally
            {
                loader.Unload();
            }
        }

        return count;
    }

    private PluginEntity CreatePluginEntity(
        PluginInstallMetadata metadata,
        string dllPath,
        IPlugin plugin)
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
            Specifications = plugin.GetPluginSpecification()
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