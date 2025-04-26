using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Application.Serialization;
using System.Security.Cryptography;
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
    private readonly IPluginLoader _pluginLoader;

    public PluginTypeService(ILogger<PluginTypeService> logger, IPluginConfigurationService pluginConfigurationService, 
        IPluginService pluginService, IJsonDeserializer deserializer, IJsonSerializer serializer, 
        IPluginCacheService pluginCacheService, IPluginLoader pluginLoader)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(pluginCacheService);
        ArgumentNullException.ThrowIfNull(pluginLoader);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _deserializer = deserializer;
        _serializer = serializer;
        _pluginCacheService = pluginCacheService;
        _pluginLoader = pluginLoader;
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

    public Task<IPlugin> Resolve(string userId, object? type, CancellationToken cancellationToken)
    {
        return type is string str
            ? ResolveByConfig(userId, str, cancellationToken)
            : ResolveByType(userId, type, cancellationToken);
    }

    private async Task<IPlugin> ResolveByConfig(string userId, string? configName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configName))
            return await GetLocalPlugin(userId);

        if (!await _pluginConfigurationService.IsExist(userId, configName, cancellationToken))
            throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, 
                string.Format(Resources.PluginTypeService_ConfigurationCouldNotFound, configName));

        var config = await _pluginConfigurationService.Get(userId, configName, cancellationToken);
        var pluginEntity = await _pluginService.Get(userId, config.Type, config.Version, cancellationToken);
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

        var pluginEntity = await _pluginService.Get(userId, typeConfig.Plugin, typeConfig.Version, cancellationToken);
        return await GetOrLoadPlugin(pluginEntity, typeConfig.Plugin, typeConfig.Specifications, cancellationToken);
    }

    private async Task<IPlugin> GetLocalPlugin(string userId)
    {
        var localFileSystemPlugin = new LocalFileSystemPlugin();
        var key = GenerateKey(localFileSystemPlugin.Metadata.Id, userId, "LocalFileSystem", null);
        var cached = _pluginCacheService.Get(key);

        if (cached != null)
            return cached;
        
        await localFileSystemPlugin.Initialize(new PluginLoggerAdapter(_logger));
        _pluginCacheService.Set(key, localFileSystemPlugin);
        return localFileSystemPlugin;
    }

    private async Task<IPlugin> GetOrLoadPlugin(PluginEntity pluginEntity, string configName, PluginSpecifications? specifications, CancellationToken cancellationToken)
    {
        var key = GenerateKey(pluginEntity.Id, pluginEntity.UserId, configName, specifications);
        var cached = _pluginCacheService.Get(key);

        if (cached != null)
            return cached;

        var pluginHandle = _pluginLoader.LoadPlugin(pluginEntity.PluginLocation);
        if (!pluginHandle.Success)
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, string.Format(Resources.PluginTypeService_PluginCouldNotFound, pluginEntity.Name));
        
        var plugin = pluginHandle.Instance;
        plugin.Specifications = specifications;
        await plugin.Initialize(new PluginLoggerAdapter(_logger));
        _pluginCacheService.Set(key, plugin);
        return plugin;
    }

    private string GenerateKey(Guid pluginId, string userId, string configName, object? specifications)
    {
        var key = $"{pluginId}-{userId}-{configName}";
        if (specifications != null)
            key += $"-{_serializer.Serialize(specifications)}";

        return HashKey(key);
    }

    private string HashKey(string? key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            using var hasher = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(key);
            var hashBytes = hasher.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}