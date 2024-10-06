using EnsureThat;
using FlowSynx.Configuration;
using FlowSynx.Core.Exceptions;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.IO.Cache;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Manager;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Parers.Contex;

internal class PluginContextParser : IPluginContextParser
{
    private readonly ILogger<PluginContextParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IPluginsManager _pluginsManager;
    private readonly ILogger<LocalFileSystemStorage> _localStorageLogger;
    private readonly INamespaceParser _namespaceParser;
    private readonly ICache<string, PluginBase> _cache;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private const string ParserSeparator = "://";
    private const string PipelineSeparator = "|";

    public PluginContextParser(ILogger<PluginContextParser> logger, IConfigurationManager configurationManager,
        IPluginsManager pluginsManager, ILogger<LocalFileSystemStorage> localStorageLogger,
        INamespaceParser namespaceParser, ICache<string, PluginBase> cache, 
        ISerializer serializer, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        EnsureArg.IsNotNull(localStorageLogger, nameof(localStorageLogger));
        EnsureArg.IsNotNull(namespaceParser, nameof(namespaceParser));
        EnsureArg.IsNotNull(cache, nameof(cache));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;
        _configurationManager = configurationManager;
        _pluginsManager = pluginsManager;
        _localStorageLogger = localStorageLogger;
        _namespaceParser = namespaceParser;
        _cache = cache;
        _serializer = serializer;
        _serviceProvider = serviceProvider;
    }

    public PluginContext Parse(string path)
    {
        try
        {
            var entity = string.Empty;
            var segments = path.Split(ParserSeparator);

            if (segments.Length != 2)
            {
                var localFileSystemPlugin = GetPluginBase(LocalFileSystemStoragePlugin(), "LocalFileSystem", null);
                return CreatePluginContext(localFileSystemPlugin, null, path);
            }

            entity = segments[1];
            var pipelineSegments = segments[0].Split(PipelineSeparator);
            if (pipelineSegments.Length > 2)
                throw new StorageNormsParserException("Pipeline is not applied correctly.");

            string primaryConfigName = string.Empty;
            string secondaryConfigName = string.Empty;
            if (pipelineSegments.Length == 1)
            {
                primaryConfigName = pipelineSegments[0];
            }
            else
            {
                primaryConfigName = pipelineSegments[0];
                secondaryConfigName = pipelineSegments[1];
            }

            var primaryConfigNameExist = _configurationManager.IsExist(primaryConfigName);

            if (!primaryConfigNameExist)
                throw new StorageNormsParserException($"{primaryConfigName} is not exist.");

            var primaryConfig = _configurationManager.Get(primaryConfigName);
            if (_namespaceParser.Parse(primaryConfig.Type) == PluginNamespace.Stream)
            {
                var secondaryConfigNameExist = _configurationManager.IsExist(secondaryConfigName);
                if (!secondaryConfigNameExist)
                    throw new StorageNormsParserException($"{secondaryConfigName} is not exist.");
            }

            var secondaryConfig = _configurationManager.Get(secondaryConfigName);

            var primaryPlugin = _pluginsManager.Get(primaryConfig.Type);
            var secondaryPlugin = _pluginsManager.Get(secondaryConfig.Type);

            var invokePlugin = GetPluginBase(primaryPlugin, primaryConfigName, primaryConfig.Specifications.ToSpecifications());
            var inferiorPlugin = GetPluginBase(secondaryPlugin, secondaryConfigName, secondaryConfig.Specifications.ToSpecifications());

            return CreatePluginContext(invokePlugin, inferiorPlugin, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }

    private LocalFileSystemStorage LocalFileSystemStoragePlugin()
    {
        var ipluginsServices = _serviceProvider.GetServices<PluginBase>();
        var localFileSystem = ipluginsServices.First(o => o.GetType() == typeof(LocalFileSystemStorage));
        return (LocalFileSystemStorage)localFileSystem;
    }

    private PluginBase GetPluginBase(PluginBase plugin, string configName, PluginSpecifications? specifications)
    {
        var key = GenerateKey(plugin.Id, configName, specifications);
        var cachedPluginContext = _cache.Get(key);

        if (cachedPluginContext != null)
        {
            _logger.LogInformation($"{plugin.Name} is found in Cache.");
            return cachedPluginContext;
        }

        plugin.Specifications = specifications;
        plugin.Initialize();
        _cache.Set(key, plugin);
        return plugin;
    }

    private PluginContext CreatePluginContext(PluginBase invokePlugin, PluginBase? inferiorPlugin, string entity)
    {
        return new PluginContext(invokePlugin, inferiorPlugin, entity);
    }

    private string GenerateKey(Guid pluginId, string configName, object? pluginSpecifications)
    {
        var key = $"{pluginId}-{configName}";
        var result = pluginSpecifications == null ? key : $"{key}-{_serializer.Serialize(pluginSpecifications)}";
        return Security.HashHelper.Md5.GetHash(result);
    }

    public void Dispose() { }
}