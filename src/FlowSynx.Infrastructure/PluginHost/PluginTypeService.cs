using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Application.Serialization;
using FlowSynx.Plugins.LocalFileSystem;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginTypeService : IPluginTypeService
{
    private readonly ILogger<PluginTypeService> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly IPluginService _pluginService;
    private readonly IJsonDeserializer _deserializer;
    private readonly IJsonSerializer _serializer;
    private readonly IPluginCacheService _pluginCacheService;
    //private readonly IPluginLoader _pluginLoader;
    private readonly IPluginCacheKeyGeneratorService _pluginCacheKeyGeneratorService;

    public PluginTypeService(
        ILogger<PluginTypeService> logger, 
        IPluginConfigurationService pluginConfigurationService, 
        IPluginService pluginService, 
        IJsonDeserializer deserializer, 
        IJsonSerializer serializer, 
        IPluginCacheService pluginCacheService, 
        //IPluginLoader pluginLoader,
        IPluginCacheKeyGeneratorService pluginCacheKeyGeneratorService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(pluginCacheService);
        //ArgumentNullException.ThrowIfNull(pluginLoader);
        ArgumentNullException.ThrowIfNull(pluginCacheKeyGeneratorService);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _deserializer = deserializer;
        _serializer = serializer;
        _pluginCacheService = pluginCacheService;
        //_pluginLoader = pluginLoader;
        _pluginCacheKeyGeneratorService = pluginCacheKeyGeneratorService;
    }

    public Task<IPlugin> Get(string userId, object? type, CancellationToken cancellationToken)
    {
        try
        {
            return type is string str
                ? ResolveByConfig(userId, str, cancellationToken)
                : ResolveByType(userId, type, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task<IPlugin> ResolveByConfig(string userId, string? configName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configName))
            return await GetLocalPlugin(userId);

        var config = await _pluginConfigurationService.Get(userId, configName, cancellationToken) 
            ?? throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, 
                string.Format(Resources.PluginTypeService_ConfigurationCouldNotFound, configName));

        var pluginEntity = await _pluginService.Get(userId, config.Type, config.Version, cancellationToken) 
            ?? throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound, 
                string.Format(Resources.PluginTypeService_PluginCouldNotFound, config.Type, config.Version));

        var specs = config.Specifications.ToPluginSpecifications();
        return await GetOrLoadPlugin(pluginEntity, configName, specs, cancellationToken);
    }

    private async Task<IPlugin> ResolveByType(string userId, object? type, CancellationToken cancellationToken)
    {
        if (type is null)
            return await GetLocalPlugin(userId);

        var typeConfig = _deserializer.Deserialize<PluginTypeConfiguration>(type.ToString());
        if (string.IsNullOrEmpty(typeConfig.Plugin))
            return await GetLocalPlugin(userId);

        var pluginEntity = await _pluginService.Get(userId, typeConfig.Plugin, typeConfig.Version, cancellationToken)
            ?? throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound,
                string.Format(Resources.PluginTypeService_PluginCouldNotFound, typeConfig.Plugin, typeConfig.Version));

        return await GetOrLoadPlugin(pluginEntity, typeConfig.Plugin, typeConfig.Specifications, cancellationToken);
    }

    private async Task<IPlugin> GetLocalPlugin(string userId)
    {
        var localFileSystemPlugin = new LocalFileSystemPlugin();

        var pluginType = localFileSystemPlugin.Metadata.Type;
        var pluginVersion = localFileSystemPlugin.Metadata.Version.ToString();

        var key = _pluginCacheKeyGeneratorService.GenerateKey(userId, pluginType, pluginVersion, null);

        var cached = _pluginCacheService.Get(key);
        if (cached != null)
            return cached.Plugin;

        var index = new PluginCacheIndex(userId, pluginType, pluginVersion);
        await localFileSystemPlugin.Initialize(new PluginLoggerAdapter(_logger));
        _pluginCacheService.Set(key, index, localFileSystemPlugin, TimeSpan.FromHours(2), TimeSpan.FromMinutes(15));
        return localFileSystemPlugin;
    }

    private async Task<IPlugin> GetOrLoadPlugin(
        PluginEntity pluginEntity, 
        string configName, 
        PluginSpecifications? specifications, 
        CancellationToken cancellationToken)
    {
        var userId = pluginEntity.UserId;
        var pluginType = pluginEntity.Type;
        var pluginVersion = pluginEntity.Version;

        var key = _pluginCacheKeyGeneratorService.GenerateKey(userId, pluginType, pluginVersion, specifications);

        var cached = _pluginCacheService.Get(key);
        if (cached != null)
            return cached.Plugin;

        try
        {
            var pluginResult = new PluginLoader(pluginEntity.PluginLocation);
            //if (!pluginResult.Succeeded)
            //    throw new FlowSynxException((int)ErrorCode.PluginLoader, string.Join(Environment.NewLine, pluginResult.ErrorMessage));

            //if (pluginResult.PluginInstance == null)
            //    throw new FlowSynxException((int)ErrorCode.PluginNotFound, "Plugin not found.");

            var index = new PluginCacheIndex(userId, pluginType, pluginVersion);
            pluginResult.Plugin.Specifications = specifications;
            await pluginResult.Plugin.Initialize(new PluginLoggerAdapter(_logger));
            _pluginCacheService.Set(key, index, pluginResult.Plugin, TimeSpan.FromHours(2), TimeSpan.FromMinutes(15));
            return pluginResult.Plugin;
        }
        catch (Exception)
        {
            throw new FlowSynxException((int)ErrorCode.PluginCouldNotLoad, 
                string.Format(Resources.PluginTypeService_PluginCouldNotLoad, pluginEntity.Name, pluginEntity.Version));
        }
    }
}