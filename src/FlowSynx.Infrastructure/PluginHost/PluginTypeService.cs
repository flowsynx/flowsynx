using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Application.Serialization;
using FlowSynx.Infrastructure.PluginHost.PluginLoaders;
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

        var key = _pluginCacheKeyGeneratorService.GenerateKey(userId, pluginType, pluginVersion, specification);

        var cached = _pluginCacheService.Get(key);
        if (cached != null)
            return cached.Plugin;

        var loader = new IsolatedPluginLoader(pluginEntity.PluginLocation);

        try
        {
            loader.Load();
            var index = new PluginCacheIndex(userId, pluginType, pluginVersion);

            if (specification == null)
                specification = new Dictionary<string, object?>();

            await loader.Plugin.InitializeAsync(new PluginLoggerAdapter(_logger), specification);
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