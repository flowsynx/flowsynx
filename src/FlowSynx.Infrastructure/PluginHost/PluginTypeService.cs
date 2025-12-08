using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Application.Serialization;
using FlowSynx.Infrastructure.PluginHost.Loader;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginTypeService : IPluginTypeService
{
    private readonly ILogger<PluginTypeService> _logger;
    private readonly IPluginService _pluginService;
    private readonly IJsonDeserializer _deserializer;
    private readonly IPluginCacheService _pluginCacheService;
    private readonly IPluginCacheKeyGeneratorService _pluginCacheKeyGeneratorService;
    private readonly ILocalization _localization;

    public PluginTypeService(
        ILogger<PluginTypeService> logger,
        IPluginService pluginService, 
        IJsonDeserializer deserializer, 
        IJsonSerializer serializer, 
        IPluginCacheService pluginCacheService,
        IPluginCacheKeyGeneratorService pluginCacheKeyGeneratorService,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _pluginCacheService = pluginCacheService ?? throw new ArgumentNullException(nameof(pluginCacheService));
        _pluginCacheKeyGeneratorService = pluginCacheKeyGeneratorService ?? throw new ArgumentNullException(nameof(pluginCacheKeyGeneratorService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public Task<IPlugin> Get(
        string userId,
        string? type,
        Dictionary<string, object?>? specification,
        CancellationToken cancellationToken)
    {
        try
        {
            return Resolve(userId, type, specification, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task<IPlugin> Resolve(
        string userId, 
        string? type,
        Dictionary<string, object?>? specification,
        CancellationToken cancellationToken)
    {
        ValidateType(type);

        var (plugin, version) = ParseType(type);
        ValidatePluginInTypeConfig((plugin, version));

        var pluginEntity = await GetPluginEntity(userId, plugin, version, cancellationToken);
        return await GetOrLoadPlugin(pluginEntity, specification);
    }

    private void ValidateType(string? type)
    {
        if (type is null)
        {
            throw new FlowSynxException((int)ErrorCode.PluginTypeShouldHaveValue,
                _localization.Get("PluginTypeService_PluginTypeShouldHaveValue"));
        }
    }

    private static (string plugin, string? version) ParseType(string type)
    {
        var parts = type.Split(':');
        var plugin = parts[0];
        var version = parts.Length > 1 ? parts[1] : null;

        return (plugin, version);
    }

    private void ValidatePluginInTypeConfig((string plugin, string? version) pluginInfo)
    {
        if (string.IsNullOrWhiteSpace(pluginInfo.plugin))
        {
            throw new FlowSynxException((int)ErrorCode.PluginTypeShouldHaveValue,
                _localization.Get("PluginTypeService_PluginTypeShouldHaveValue"));
        }
    }

    private async Task<PluginEntity> GetPluginEntity(
        string userId,
        string? plugin,
        string? version,
        CancellationToken cancellationToken)
    {
        var pluginEntity = await _pluginService.Get(userId, plugin, version, cancellationToken);

        if (pluginEntity is null)
        {
            throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound,
                _localization.Get("PluginTypeService_PluginCouldNotFound", plugin, version));
        }

        return pluginEntity;
    }

    private async Task<IPlugin> GetOrLoadPlugin(
    PluginEntity pluginEntity,
    Dictionary<string, object?>? specification)
    {
        var userId = pluginEntity.UserId;
        var pluginType = pluginEntity.Type;
        var pluginVersion = pluginEntity.Version;

        var key = _pluginCacheKeyGeneratorService.GenerateKey(
            userId, pluginType, pluginVersion, specification);

        // Check cache
        var cached = _pluginCacheService.Get(key);
        if (cached != null)
        {
            var instance = cached.GetPluginInstance();
            if (instance != null)
                return instance;
        }

        // Create new loader
        var loader = new PluginLoader(pluginEntity.PluginLocation);
        PluginLoadResult loadResult;

        try
        {
            loadResult = await loader.LoadAsync();
        }
        catch
        {
            await loader.UnloadAsync();
            throw new FlowSynxException(
                (int)ErrorCode.PluginCouldNotLoad,
                _localization.Get(
                    "PluginTypeService_PluginCouldNotLoad",
                    pluginType,
                    pluginVersion));
        }

        // LoadAsync succeeded but plugin may be null → fail safely
        if (!loadResult.Success || loadResult.PluginInstance == null)
        {
            await loader.UnloadAsync();
            throw new FlowSynxException(
                (int)ErrorCode.PluginCouldNotLoad,
                _localization.Get(
                    "PluginTypeService_PluginCouldNotLoad",
                    pluginType,
                    pluginVersion));
        }

        var pluginInstance = loadResult.PluginInstance;

        // Ensure specification exists
        specification ??= new Dictionary<string, object?>();

        try
        {
            // Always initialize after load
            await pluginInstance.InitializeAsync(
                new PluginLoggerAdapter(_logger),
                specification);
        }
        catch
        {
            await loader.UnloadAsync();
            throw new FlowSynxException(
                (int)ErrorCode.PluginCouldNotLoad,
                _localization.Get(
                    "PluginTypeService_PluginCouldNotLoad",
                    pluginType,
                    pluginVersion));
        }

        // Cache the loader (so the ALC stays alive)
        var index = new PluginCacheIndex(userId, pluginType, pluginVersion);

        _pluginCacheService.Set(
            key,
            index,
            loader,
            TimeSpan.FromHours(2),
            TimeSpan.FromMinutes(15));

        return pluginInstance;
    }
}