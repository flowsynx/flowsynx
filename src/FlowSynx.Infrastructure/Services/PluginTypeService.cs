using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Plugins.LocalFileSystem;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;

namespace FlowSynx.Infrastructure.Services;

public class PluginTypeService : IPluginTypeService
{
    private readonly ILogger<PluginTypeService> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly IPluginService _pluginService;
    private readonly ICacheService<string, Plugin> _cacheService;
    private readonly IJsonSerializer _serializer;
    private readonly IJsonDeserializer _deserializer;
    private readonly IHashService _hashService;
    private readonly IServiceProvider _serviceProvider;

    public PluginTypeService(ILogger<PluginTypeService> logger, IPluginConfigurationService pluginConfigurationService,
        IPluginService pluginService, ICacheService<string, Plugin> cacheService, 
        IJsonSerializer serializer, IJsonDeserializer deserializer,
        IHashService hashService, IServiceProvider serviceProvider)
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
    }

    public Task<Plugin> Get(string userId, object? type, CancellationToken cancellationToken)
    {
        try
        {
            return type is string 
                ? GetPluginBasedOnConfig(userId, type.ToString(), cancellationToken) 
                : GetPluginBasedOnType(type, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeGetItem, ex.Message)
            var exception = new FlowSynxException((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(exception.ToString());
            throw exception;
        }
    }

    private async Task<Plugin> GetPluginBasedOnConfig(string userId, string? configName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configName))
        {
            var localFileSystemPlugin = GetPlugin(LocalFileSystemConnector(), "LocalFileSystem", null);
            return localFileSystemPlugin;
        }

        var currentConfigExist = await _pluginConfigurationService.IsExist(userId, configName, cancellationToken);

        if (!currentConfigExist)
            throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, $"Configuration '{configName}' could be not found.");

        var currentConfig = await _pluginConfigurationService.Get(userId, configName, cancellationToken);
        var getCurrentPlugin = await _pluginService.Get(currentConfig.Type, cancellationToken);
        var currentPlugin = GetPlugin(getCurrentPlugin, configName, currentConfig.Specifications.ToPluginSpecifications());

        return currentPlugin;
    }

    private async Task<Plugin> GetPluginBasedOnType(object? type, CancellationToken cancellationToken)
    {
        try
        {
            string? connectorType;
            PluginSpecifications? specifications = null;

            if (type is null)
            {
                connectorType = string.Empty;
            }
            else
            {
                var conn = _deserializer.Deserialize<TypeConfiguration>(type.ToString());
                connectorType = conn.Connector;
                specifications = conn.Specifications;
            }

            if (string.IsNullOrEmpty(connectorType))
            {
                var localFileSystemConnector = GetPlugin(LocalFileSystemConnector(), "LocalFileSystem", specifications);
                return localFileSystemConnector;
            }
            
            var getCurrentConnector = await _pluginService.Get(connectorType, cancellationToken);
            var currentConnector = GetPlugin(getCurrentConnector, connectorType, specifications);

            return currentConnector;
        }
        catch (Exception ex)
        {
            var exception = new FlowSynxException((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(exception.ToString());
            throw exception;
        }
    }

    private LocalFileSystemPlugin LocalFileSystemConnector()
    {
        var iConnectorsServices = _serviceProvider.GetServices<Plugin>();
        var localFileSystem = iConnectorsServices.First(o => o.GetType() == typeof(LocalFileSystemPlugin));
        return (LocalFileSystemPlugin)localFileSystem;
    }

    private Plugin GetPlugin(Plugin plugin, string configName, PluginSpecifications? specifications)
    {
        var key = GenerateKey(plugin.Id, configName, specifications);
        var cachedConnectorContext = _cacheService.Get(key);

        if (cachedConnectorContext != null)
        {
            _logger.LogInformation($"{plugin.Name} is found in Cache.");
            return cachedConnectorContext;
        }

        plugin.Specifications = specifications;
        plugin.Initialize();
        _cacheService.Set(key, plugin);
        return plugin;
    }

    private string GenerateKey(Guid pluginId, string configName, object? connectorSpecifications)
    {
        var key = $"{pluginId}-{configName}";
        var result = connectorSpecifications == null ? key : $"{key}-{_serializer.Serialize(connectorSpecifications)}";
        return _hashService.Hash(result);
    }

    public void Dispose() { }
}

public class TypeConfiguration
{
    public string? Connector { get; set; }
    public PluginSpecifications? Specifications { get; set; }
}