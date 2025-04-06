using FlowSynx.Application.Configuration;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.Plugin;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginRegistryConfiguration _pluginRegistryConfiguration;
    private readonly IPluginsLocation _pluginsLocation;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginDownloader _pluginDownloader;
    private readonly IPluginChecksumValidator _pluginChecksumValidator;
    private readonly IPluginExtractor _pluginExtractor;
    private readonly IExtractPluginSpecifications _extractPluginSpecifications;

    public PluginManager(ILogger<PluginManager> logger, PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation, ICurrentUserService currentUserService, IPluginService pluginService, 
        IPluginDownloader pluginDownloader, IPluginChecksumValidator pluginChecksumValidator, 
        IPluginExtractor pluginExtractor, IExtractPluginSpecifications extractPluginSpecifications)
    {
        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration;
        _pluginsLocation = pluginsLocation;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginDownloader = pluginDownloader;
        _pluginChecksumValidator = pluginChecksumValidator;
        _pluginExtractor = pluginExtractor;
        _extractPluginSpecifications = extractPluginSpecifications;
    }

    public async Task InstallAsync(string pluginName, string pluginVersion, CancellationToken cancellationToken)
    {
        var registryUrl = _pluginRegistryConfiguration.Url;
        string pluginMetadataUrl = $"{registryUrl}/{pluginName}/{pluginVersion}";
        string pluginDataUrl = $"{registryUrl}/{pluginName}/{pluginVersion}/Download";

        var rootLocation = Path.Combine(_pluginsLocation.Path, _currentUserService.UserId);
        string pluginLocalPath = Path.Combine(rootLocation, pluginName, pluginVersion, $"{pluginName}.{pluginVersion}.zip");

        var pluginMetadata = await _pluginDownloader.GetPluginMetadataAsync(pluginMetadataUrl);
        var pluginData = await _pluginDownloader.GetPluginDataAsync(pluginDataUrl);

        bool isChecksumValid = _pluginChecksumValidator.ValidateChecksum(pluginData, pluginMetadata.Checksum);
        if (!isChecksumValid)
        {
            _logger.LogError("Checksum validation failed. Package may be corrupted or tampered with.");
            return;
        }

        var pluginLocation = await _pluginExtractor.ExtractPluginAsync(pluginLocalPath, cancellationToken);
        File.Delete(pluginLocalPath);

        var targetType = typeof(IPlugin);
        foreach (var dllPath in Directory.GetFiles(pluginLocation, "*.dll"))
        {
            _logger.LogInformation($"\nScanning: {Path.GetFileName(dllPath)}");

            var pluginLoadContext = new PluginLoadContext(dllPath);
            try
            {
                var assembly = pluginLoadContext.LoadFromAssemblyPath(dllPath);

                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsAbstract && targetType.IsAssignableFrom(type))
                    {
                        var pluginInstance = Activator.CreateInstance(type);
                        var targetPlugin = pluginInstance as IPlugin;

                        var pluginEntity = new PluginEntity
                        {
                            Id = Guid.NewGuid(),
                            PluginId = pluginMetadata.Id,
                            Name = pluginMetadata.Name,
                            Version = pluginMetadata.Version,
                            Checksum = pluginMetadata.Checksum,
                            PluginLocation = pluginLocation,
                            UserId = _currentUserService.UserId,
                            Author = pluginMetadata.Author,
                            Description = pluginMetadata.Description,
                            Type = pluginMetadata.Type,
                            Specifications = _extractPluginSpecifications.GetPluginSpecification(targetPlugin)
                        };

                        await _pluginService.Add(pluginEntity, cancellationToken);
                        _logger.LogInformation($"Plugin {pluginName} v{pluginVersion} installed successfully and saved to database.");
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogError($"⚠️ Could not load all types from {dllPath}: {ex.Message}");
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    _logger.LogError($" - {loaderEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error loading {dllPath}: {ex.Message}");
            }
        }
    }

    public async Task UpdateAsync(string pluginName, string oldVersion, string newPluginVersion, CancellationToken cancellationToken)
    {
        await Uninstall(pluginName, oldVersion, cancellationToken);
        await InstallAsync(pluginName, newPluginVersion, cancellationToken);
    }

    public async Task Uninstall(string pluginName, string version, CancellationToken cancellationToken)
    {
        var pluginEntity = await _pluginService.Get(_currentUserService.UserId, pluginName, version, cancellationToken);
        if (pluginEntity != null)
        {
            var pluginLocation = pluginEntity.PluginLocation;
            if (Directory.Exists(pluginLocation))
            {
                Directory.Delete(pluginLocation, true);
                _logger.LogInformation($"Uninstalled: {pluginName}");
            }
        }
    }
}