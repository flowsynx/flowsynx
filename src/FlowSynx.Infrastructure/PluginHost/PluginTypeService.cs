using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Application.Serialization;
using FlowSynx.Infrastructure.PluginHost.PluginLoaders;
using FlowSynx.Plugins.LocalFileSystem;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginTypeService : IPluginTypeService
{
    private readonly ILogger<PluginTypeService> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly IPluginService _pluginService;
    private readonly IJsonDeserializer _deserializer;
    private readonly IPluginCacheService _pluginCacheService;
    private readonly IPluginCacheKeyGeneratorService _pluginCacheKeyGeneratorService;
    private readonly ILocalization _localization;

    public PluginTypeService(
        ILogger<PluginTypeService> logger, 
        IPluginConfigurationService pluginConfigurationService, 
        IPluginService pluginService, 
        IJsonDeserializer deserializer, 
        IJsonSerializer serializer, 
        IPluginCacheService pluginCacheService,
        IPluginCacheKeyGeneratorService pluginCacheKeyGeneratorService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(pluginCacheService);
        ArgumentNullException.ThrowIfNull(pluginCacheKeyGeneratorService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _deserializer = deserializer;
        _pluginCacheService = pluginCacheService;
        _pluginCacheKeyGeneratorService = pluginCacheKeyGeneratorService;
        _localization = localization;
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
                _localization.Get("PluginTypeService_ConfigurationCouldNotFound", configName));

        var pluginEntity = await _pluginService.Get(userId, config.Type, config.Version, cancellationToken) 
            ?? throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound,
                _localization.Get("PluginTypeService_PluginCouldNotFound", config.Type, config.Version));

        var specs = config.Specifications.ToPluginSpecifications();
        return await GetOrLoadPlugin(pluginEntity, specs);
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
                _localization.Get("PluginTypeService_PluginCouldNotFound", typeConfig.Plugin, typeConfig.Version));

        return await GetOrLoadPlugin(pluginEntity, typeConfig.Specifications);
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
        var localPluginLoader = new DirectPluginReferenceLoader(localFileSystemPlugin);
        _pluginCacheService.Set(key, index, localPluginLoader, TimeSpan.FromHours(2), TimeSpan.FromMinutes(15));
        return localFileSystemPlugin;
    }

    private async Task<IPlugin> GetOrLoadPlugin(
        PluginEntity pluginEntity,
        PluginSpecifications? specifications)
    {
        var userId = pluginEntity.UserId;
        var pluginType = pluginEntity.Type;
        var pluginVersion = pluginEntity.Version;

        var key = _pluginCacheKeyGeneratorService.GenerateKey(userId, pluginType, pluginVersion, specifications);

        var cached = _pluginCacheService.Get(key);
        if (cached != null)
            return cached.Plugin;

        var loader = new IsolatedPluginLoader(pluginEntity.PluginLocation);

        try
        {
            loader.Load();
            var index = new PluginCacheIndex(userId, pluginType, pluginVersion);
            loader.Plugin.Specifications = specifications;
            await loader.Plugin.Initialize(new PluginLoggerAdapter(_logger));
            _pluginCacheService.Set(key, index, loader, TimeSpan.FromHours(2), TimeSpan.FromMinutes(15));
            return loader.Plugin;
        }
        catch (Exception)
        {
            loader.Unload();
            throw new FlowSynxException((int)ErrorCode.PluginCouldNotLoad,
                _localization.Get("PluginTypeService_PluginCouldNotLoad", pluginEntity.Type, pluginEntity.Version));
        }
    }
}