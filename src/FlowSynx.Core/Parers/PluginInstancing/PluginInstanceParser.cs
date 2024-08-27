using System.Reflection;
using EnsureThat;
using FlowSynx.Configuration;
using FlowSynx.Core.Exceptions;
using FlowSynx.Core.Extensions;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.IO.Cache;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Manager;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using FlowSynx.Reflections;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Parers.PluginInstancing;

internal class PluginInstanceParser : IPluginInstanceParser
{
    private readonly ILogger<PluginInstanceParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IPluginsManager _pluginsManager;
    private readonly ILogger<LocalFileSystemStorage> _localStorageLogger;
    private readonly IStorageFilter _storageFilter;
    private readonly INamespaceParser _namespaceParser;
    private readonly IMultiKeyCache<string, string, PluginInstance> _multiKeyCache;
    private readonly ISerializer _serializer;
    private const string ParserSeparator = "://";

    public PluginInstanceParser(ILogger<PluginInstanceParser> logger, IConfigurationManager configurationManager,
        IPluginsManager pluginsManager, ILogger<LocalFileSystemStorage> localStorageLogger,
        IStorageFilter storageFilter, INamespaceParser namespaceParser,
        IMultiKeyCache<string, string, PluginInstance> multiKeyCache, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        EnsureArg.IsNotNull(localStorageLogger, nameof(localStorageLogger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        EnsureArg.IsNotNull(namespaceParser, nameof(namespaceParser));
        EnsureArg.IsNotNull(multiKeyCache, nameof(multiKeyCache));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        _logger = logger;
        _configurationManager = configurationManager;
        _pluginsManager = pluginsManager;
        _localStorageLogger = localStorageLogger;
        _storageFilter = storageFilter;
        _namespaceParser = namespaceParser;
        _multiKeyCache = multiKeyCache;
        _serializer = serializer;
    }

    public PluginInstance Parse(string path)
    {
        try
        {
            var segments = path.Split(ParserSeparator);

            if (segments.Length != 2)
            {
                return CreateStorageNormsInfoInstance(GetLocalFileSystemStoragePlugin(), "LocalFileSystem", null, path);
            }

            var fileSystemExist = _configurationManager.IsExist(segments[0]);
            if (!fileSystemExist)
            {
                return CreateStorageNormsInfoInstance(GetLocalFileSystemStoragePlugin(), "LocalFileSystem", null, path);
            }

            var fileSystem = _configurationManager.Get(segments[0]);

            if (_namespaceParser.Parse(fileSystem.Type) != PluginNamespace.Storage)
                throw new StorageNormsParserException(string.Format(Resources.StorageNormsParserInvalidStorageType, fileSystem.Type));

            //var specifications = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            //if (fileSystem.Specifications != null)
            //    specifications = fileSystem.Specifications;

            var plugin = _pluginsManager.Get(fileSystem.Type);
            return CreateStorageNormsInfoInstance(plugin, segments[0], fileSystem.Specifications.ToSpecifications(), segments[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }

    private LocalFileSystemStorage GetLocalFileSystemStoragePlugin()
    {
        return new LocalFileSystemStorage(_localStorageLogger, _storageFilter);
    }

    private PluginInstance CreateStorageNormsInfoInstance(IPlugin plugin, string configName, PluginSpecifications? specifications, string entity)
    {
        var primaryKey = plugin.Id.ToString();
        var secondaryKey = GenerateSecondaryKey(configName, null);
        var cachedStorageNormsInfo = _multiKeyCache.Get(primaryKey, secondaryKey);

        if (cachedStorageNormsInfo != null)
        {
            _logger.LogInformation($"{plugin.Name} is found in Cache.");
            cachedStorageNormsInfo.Entity = entity;
            return cachedStorageNormsInfo;
        }

        var storageNormsInfo = new PluginInstance(plugin.CastTo<IPlugin>(), entity, specifications);
        _multiKeyCache.Set(primaryKey, secondaryKey, storageNormsInfo);
        storageNormsInfo.Initialize();
        return storageNormsInfo;
    }

    private string GenerateSecondaryKey(string configName, object? specifications)
    {
        var specificationsValue = specifications == null ? $"{configName}" : $"{configName}-{_serializer.Serialize(specifications)}";
        return Security.HashHelper.Md5.GetHash(specificationsValue);
    }

    public void Dispose() { }
}