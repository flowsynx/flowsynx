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
using System.IO.Compression;

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
    private readonly IVersion _version;
    private const string PluginSearchPattern = "*.dll";

    public PluginManager(
        ILogger<PluginManager> logger,
        PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation,
        ICurrentUserService currentUserService,
        IPluginService pluginService,
        IPluginDownloader pluginDownloader,
        IPluginCacheService pluginCacheService,
        ILocalization localization,
        IVersion version)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginRegistryConfiguration);
        ArgumentNullException.ThrowIfNull(pluginsLocation);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(pluginDownloader);
        ArgumentNullException.ThrowIfNull(pluginCacheService);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(version);

        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration;
        _pluginsLocation = pluginsLocation;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginDownloader = pluginDownloader;
        _pluginCacheService = pluginCacheService;
        _localization = localization;
        _version = version;
    }

    public async Task InstallAsync(string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        if (await PluginAlreadyExists(pluginType, pluginVersion, cancellationToken))
            return;

        var registryUrl = _pluginRegistryConfiguration.Url;
        var pluginMetadata = await _pluginDownloader.GetPluginMetadataAsync(registryUrl, pluginType, pluginVersion, cancellationToken);
        var pluginPackData = await _pluginDownloader.GetPluginDataAsync(registryUrl, pluginType, pluginVersion, cancellationToken);

        var pluginBytes = ExtractPluginFile(pluginPackData);
        ValidatePluginChecksum(pluginBytes, pluginMetadata.Checksum);

        var isPluginCompatible = IsPluginCompatible(pluginMetadata, _version.Version, out var warningMessage);
        if (!isPluginCompatible) 
            throw new FlowSynxException((int)ErrorCode.PluginCompatibility, warningMessage);

        if (!string.IsNullOrEmpty(warningMessage))
            _logger.LogWarning(warningMessage);

        string pluginDirectory = GetPluginLocalDirectory(pluginType, pluginVersion);
        await _pluginDownloader.ExtractPluginAsync(pluginDirectory, pluginBytes, cancellationToken);

        int installedCount = await InstallPluginAssemblies(pluginDirectory, pluginMetadata, cancellationToken);

        if (installedCount == 0)
            throw new FlowSynxException((int)ErrorCode.PluginInstallationNotFound,
                _localization.Get("Plugin_Install_NoPluginInstalled"));
    }

    private byte[] ExtractPluginFile(byte[] pluginData)
    {
        using var memoryStream = new MemoryStream(pluginData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);

        var pluginEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".plugin", StringComparison.OrdinalIgnoreCase));
        if (pluginEntry == null)
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, "No .plugin file found in the package.");

        using var entryStream = pluginEntry.Open();
        using var pluginStream = new MemoryStream();
        entryStream.CopyTo(pluginStream);
        return pluginStream.ToArray();
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

    private void ValidatePluginChecksum(byte[] pluginData, string? checksum)
    {
        if (_pluginDownloader.ValidateChecksum(pluginData, checksum))
            return;

        throw new FlowSynxException((int)ErrorCode.PluginChecksumValidationFailed, 
            _localization.Get("PluginManager_Install_ChecksumValidationFailed"));
    }

    private bool IsPluginCompatible(PluginInstallMetadata pluginMetadata, Version hostVersion, out string warning)
    {
        warning = string.Empty;

        if (!Version.TryParse(pluginMetadata.MinimumFlowSynxVersion, out var minVersion))
        {
            warning = _localization.Get("Plugin_Invalid_MinimumFlowSynxVersion", pluginMetadata.MinimumFlowSynxVersion);
            return false;
        }

        if (hostVersion < minVersion)
        {
            warning = _localization.Get("Plugin_HostVersion_IsOld", hostVersion, pluginMetadata.MinimumFlowSynxVersion);
            return false;
        }

        if (!string.IsNullOrEmpty(pluginMetadata.TargetFlowSynxVersion))
        {
            if (!Version.TryParse(pluginMetadata.TargetFlowSynxVersion, out var targetVersion))
            {
                warning = _localization.Get("Plugin_Invalid_TargetFlowSynxVersion", pluginMetadata.TargetFlowSynxVersion);
                return false;
            }

            if (hostVersion > targetVersion)
            {
                warning = _localization.Get("Plugin_HostVersion_IsNewer", pluginMetadata.Type, pluginMetadata.TargetFlowSynxVersion, hostVersion);
                return true;
            }
        }

        return true; // Fully compatible
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
            Type = metadata.Type,
            Version = metadata.Version,
            Checksum = metadata.Checksum,
            PluginLocation = dllPath,
            UserId = _currentUserService.UserId,
            Owners = metadata.Owners.ToList(),
            Description = metadata.Description,
            Copyright = metadata.Copyright,
            License = metadata.License,
            LicenseUrl = metadata.LicenseUrl,
            ProjectUrl = metadata.ProjectUrl,
            RepositoryUrl = metadata.RepositoryUrl,
            LastUpdated = metadata.LastUpdated,
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