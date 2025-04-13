using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.PluginCore;
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
    private readonly IPluginLoader _pluginLoader;

    public PluginManager(ILogger<PluginManager> logger, PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation, ICurrentUserService currentUserService, IPluginService pluginService,
        IPluginDownloader pluginDownloader, IPluginLoader pluginLoader)
    {
        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration;
        _pluginsLocation = pluginsLocation;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginDownloader = pluginDownloader;
        _pluginLoader = pluginLoader;
    }

    public async Task InstallAsync(string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        var isPlugineExist = await _pluginService.IsExist(_currentUserService.UserId, pluginType, pluginVersion, cancellationToken);
        if (isPlugineExist)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginCheckExistence, $"The plugin type '{pluginType}' with version '{pluginVersion}' is already exist.");
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }

        var registryUrl = _pluginRegistryConfiguration.Url;

        var rootLocation = Path.Combine(_pluginsLocation.Path, _currentUserService.UserId);
        string pluginLocalDirectory = Path.Combine(rootLocation, pluginType, pluginVersion);

        var pluginMetadata = await _pluginDownloader.GetPluginMetadataAsync(registryUrl, pluginType, pluginVersion);
        var pluginData = await _pluginDownloader.GetPluginDataAsync(pluginMetadata.Url);

        bool isChecksumValid = _pluginDownloader.ValidateChecksum(pluginData, pluginMetadata.Checksum);
        if (!isChecksumValid)
        {
            _logger.LogError("Checksum validation failed. Package may be corrupted or tampered with.");
            return;
        }

        await _pluginDownloader.ExtractPluginAsync(pluginLocalDirectory, pluginData, cancellationToken);

        var targetType = typeof(IPlugin);
        foreach (var pluginLocation in Directory.GetFiles(pluginLocalDirectory, "*.dll"))
        {
            try
            {
                var pluginHandle = _pluginLoader.LoadPlugin(pluginLocation);
                if (!pluginHandle.Success)
                    continue;
                
                var pluginEntity = new PluginEntity
                {
                    Id = Guid.NewGuid(),
                    PluginId = pluginMetadata.Id,
                    Name = pluginMetadata.Name,
                    Version = pluginMetadata.Version,
                    Checksum = pluginMetadata.Checksum,
                    PluginLocation = pluginHandle.Location,
                    UserId = _currentUserService.UserId,
                    Author = pluginMetadata.Author,
                    Description = pluginMetadata.Description,
                    Type = pluginMetadata.Type,
                    Specifications = pluginHandle.Instance.GetPluginSpecification()
                };

                await _pluginService.Add(pluginEntity, cancellationToken);
                _logger.LogInformation($"Plugin {pluginType} v{pluginVersion} installed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading {pluginLocation}: {ex.Message}");
            }
        }
    }

    public async Task UpdateAsync(string pluginType, string oldVersion, string newPluginVersion, CancellationToken cancellationToken)
    {
        await Uninstall(pluginType, oldVersion, cancellationToken);
        await InstallAsync(pluginType, newPluginVersion, cancellationToken);
    }

    public async Task Uninstall(string pluginType, string version, CancellationToken cancellationToken)
    {
        var pluginEntity = await _pluginService.Get(_currentUserService.UserId, pluginType, version, cancellationToken);
        if (pluginEntity != null)
        {
            var pluginLocation = pluginEntity.PluginLocation;
            if (Directory.Exists(pluginLocation))
            {
                Directory.Delete(pluginLocation, true);
                _logger.LogInformation($"Uninstalled: '{pluginType}' version '{version}'");
            }
        }
    }
}