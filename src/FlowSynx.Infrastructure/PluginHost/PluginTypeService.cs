using Microsoft.Extensions.DependencyInjection;
using FlowSynx.PluginCore;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Plugins.LocalFileSystem;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Application.Serialization;
using FlowSynx.Infrastructure.Services;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginTypeService : IPluginTypeService
{
    private readonly ILogger<PluginTypeService> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly IPluginService _pluginService;
    private readonly ICacheService<string, IPlugin> _cacheService;
    private readonly IJsonSerializer _serializer;
    private readonly IJsonDeserializer _deserializer;
    private readonly IHashService _hashService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPluginLoader _pluginLoader;

    public PluginTypeService(ILogger<PluginTypeService> logger, IPluginConfigurationService pluginConfigurationService,
        IPluginService pluginService, ICacheService<string, IPlugin> cacheService,
        IJsonSerializer serializer, IJsonDeserializer deserializer,
        IHashService hashService, IServiceProvider serviceProvider,
        IPluginLoader pluginLoader)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(hashService);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _cacheService = cacheService;
        _serializer = serializer;
        _deserializer = deserializer;
        _hashService = hashService;
        _serviceProvider = serviceProvider;
        _pluginLoader = pluginLoader;
    }

    public Task<IPlugin> Get(string userId, object? type, CancellationToken cancellationToken)
    {
        try
        {
            return type is string
                ? GetPluginBasedOnConfig(userId, type.ToString(), cancellationToken)
                : GetPluginBasedOnType(userId, type, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task<IPlugin> GetPluginBasedOnConfig(string userId, string? configName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configName))
        {
            var localFileSystemPlugin = await GetLocalFileSystemPlugin(userId);
            return localFileSystemPlugin;
        }

        var currentConfigExist = await _pluginConfigurationService.IsExist(userId, configName, cancellationToken);

        if (!currentConfigExist)
            throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, $"Configuration '{configName}' could be not found.");

        var currentConfig = await _pluginConfigurationService.Get(userId, configName, cancellationToken);
        var getCurrentPlugin = await _pluginService.Get(userId, currentConfig.Type, currentConfig.Version, cancellationToken);
        var currentPlugin = await GetPlugin(getCurrentPlugin, configName, currentConfig.Specifications.ToPluginSpecifications(),
            cancellationToken);

        return currentPlugin;
    }

    private async Task<IPlugin> GetPluginBasedOnType(string userId, object? type, CancellationToken cancellationToken)
    {
        try
        {
            string? pluginType;
            string? pluginVersion;
            PluginSpecifications? specifications = null;

            if (type is null)
            {
                pluginType = string.Empty;
                pluginVersion = string.Empty;
            }
            else
            {
                var typeConfiguration = _deserializer.Deserialize<TypeConfiguration>(type.ToString());
                pluginType = typeConfiguration.Plugin;
                pluginVersion = typeConfiguration.Version;
                specifications = typeConfiguration.Specifications;
            }

            if (string.IsNullOrEmpty(pluginType))
            {
                var localFileSystemConnector = await GetLocalFileSystemPlugin(userId);
                return localFileSystemConnector;
            }

            var getCurrentConnector = await _pluginService.Get(userId, pluginType, pluginVersion, cancellationToken);
            var currentConnector = await GetPlugin(getCurrentConnector, pluginType, specifications, cancellationToken);

            return currentConnector;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task<IPlugin> GetLocalFileSystemPlugin(string userId)
    {
        var plugins = _serviceProvider.GetServices<IPlugin>();
        var localFileSystemPlugin = plugins.First(o => o.GetType() == typeof(LocalFileSystemPlugin));

        var key = GenerateKey(localFileSystemPlugin.Metadata.Id, userId, "LocalFileSystem", null);
        var cachedConnectorContext = _cacheService.Get(key);

        if (cachedConnectorContext != null)
        {
            _logger.LogInformation($"LocalFileSystem plugin is found in Cache.");
            return cachedConnectorContext;
        }

        await localFileSystemPlugin.Initialize();
        _cacheService.Set(key, localFileSystemPlugin);
        return localFileSystemPlugin;
    }

    private async Task<IPlugin> GetPlugin(PluginEntity pluginEntity, string configName, PluginSpecifications? specifications,
    CancellationToken cancellationToken)
    {
        var key = GenerateKey(pluginEntity.Id, pluginEntity.UserId, configName, specifications);
        var cachedConnectorContext = _cacheService.Get(key);

        if (cachedConnectorContext != null)
        {
            _logger.LogInformation($"{pluginEntity.Name} is found in Cache.");
            return cachedConnectorContext;
        }

        var plugin = await _pluginLoader.LoadPlugin(pluginEntity.PluginLocation, cancellationToken);
        plugin.Specifications = specifications;
        await plugin.Initialize();
        _cacheService.Set(key, plugin);
        return plugin;
    }

    private string GenerateKey(Guid pluginId, string userId, string configName, object? specifications)
    {
        var key = $"{pluginId}-{userId}-{configName}";
        var result = specifications == null ? key : $"{key}-{_serializer.Serialize(specifications)}";
        return _hashService.Hash(result);
    }

    public void Dispose() { }
}

public class TypeConfiguration
{
    public string? Plugin { get; set; }
    public string? Version { get; set; }
    public PluginSpecifications? Specifications { get; set; }
}