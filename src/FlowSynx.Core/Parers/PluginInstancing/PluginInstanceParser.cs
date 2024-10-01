using EnsureThat;
using FlowSynx.Configuration;
using FlowSynx.Core.Exceptions;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.IO.Cache;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Manager;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Parers.PluginInstancing;

internal class PluginInstanceParser : IPluginInstanceParser
{
    private readonly ILogger<PluginInstanceParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IPluginsManager _pluginsManager;
    private readonly ILogger<LocalFileSystemStorage> _localStorageLogger;
    private readonly INamespaceParser _namespaceParser;
    private readonly IMultiKeyCache<string, string, PluginInstance> _multiKeyCache;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private const string ParserSeparator = "://";
    private const string PipelineSeparator = "|";

    public PluginInstanceParser(ILogger<PluginInstanceParser> logger, IConfigurationManager configurationManager,
        IPluginsManager pluginsManager, ILogger<LocalFileSystemStorage> localStorageLogger,
        INamespaceParser namespaceParser, IMultiKeyCache<string, string, PluginInstance> multiKeyCache, 
        ISerializer serializer, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        EnsureArg.IsNotNull(localStorageLogger, nameof(localStorageLogger));
        EnsureArg.IsNotNull(namespaceParser, nameof(namespaceParser));
        EnsureArg.IsNotNull(multiKeyCache, nameof(multiKeyCache));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;
        _configurationManager = configurationManager;
        _pluginsManager = pluginsManager;
        _localStorageLogger = localStorageLogger;
        _namespaceParser = namespaceParser;
        _multiKeyCache = multiKeyCache;
        _serializer = serializer;
        _serviceProvider = serviceProvider;
    }

    public PluginInstance Parse(string path)
    {
        try
        {
            var entity = string.Empty;
            var segments = path.Split(ParserSeparator);

            if (segments.Length != 2)
            {
                return CreateStorageNormsInfoInstance(GetLocalFileSystemStoragePlugin(), "LocalFileSystem", null, path);
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
                throw new StorageNormsParserException($"{primaryConfigName} is exist.");

            var primaryConfig = _configurationManager.Get(primaryConfigName);
            if (_namespaceParser.Parse(primaryConfig.Type) == PluginNamespace.Stream)
            {
                var secondaryConfigNameExist = _configurationManager.IsExist(secondaryConfigName);
                if (!secondaryConfigNameExist)
                    throw new StorageNormsParserException($"{secondaryConfigName} is exist.");
            }
            else if (_namespaceParser.Parse(primaryConfig.Type) != PluginNamespace.Stream && !string.IsNullOrEmpty(secondaryConfigName))
                throw new StorageNormsParserException($"{primaryConfigName} not supporting multiple entity.");

            var plugin = _pluginsManager.Get(primaryConfig.Type);
            return CreateStorageNormsInfoInstance(plugin, segments[0], primaryConfig.Specifications.ToSpecifications(), entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }

    private LocalFileSystemStorage GetLocalFileSystemStoragePlugin()
    {
        var ipluginsServices = _serviceProvider.GetServices<PluginBase>();
        var localFileSystem = ipluginsServices.First(o => o.GetType() == typeof(LocalFileSystemStorage));
        return (LocalFileSystemStorage)localFileSystem;
    }

    private PluginInstance CreateStorageNormsInfoInstance(PluginBase plugin, string configName, PluginSpecifications? specifications, string entity)
    {
        var primaryKey = plugin.Id.ToString();
        var secondaryKey = GenerateSecondaryKey(configName, specifications, entity);
        var cachedStorageNormsInfo = _multiKeyCache.Get(primaryKey, secondaryKey);

        if (cachedStorageNormsInfo != null)
        {
            _logger.LogInformation($"{plugin.Name} is found in Cache.");
            cachedStorageNormsInfo.Entity = entity;
            return cachedStorageNormsInfo;
        }

        var storageNormsInfo = new PluginInstance(plugin, entity, specifications);
        _multiKeyCache.Set(primaryKey, secondaryKey, storageNormsInfo);
        storageNormsInfo.Initialize();
        return storageNormsInfo;
    }

    private string GenerateSecondaryKey(string configName, object? specifications, string entity)
    {
        var specificationsValue = specifications == null ? $"{configName}-{entity}" : $"{configName}-{entity}-{_serializer.Serialize(specifications)}";
        return Security.HashHelper.Md5.GetHash(specificationsValue);
    }

    public void Dispose() { }
}