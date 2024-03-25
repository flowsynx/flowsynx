using System.Reflection;
using EnsureThat;
using FlowSynx.Configuration;
using FlowSynx.Core.Exceptions;
using FlowSynx.Core.Extensions;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Parers.Norms.Storage;

internal class StorageNormsParser : IStorageNormsParser
{
    private readonly ILogger<StorageNormsParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IPluginsManager _pluginsManager;
    private readonly IDeserializer _deserializer;
    private readonly ILogger<LocalFileSystemStorage> _localStorageLogger;
    private readonly IStorageFilter _storageFilter;
    private readonly INamespaceParser _namespaceParser;
    private const string ParserSeparator = "::";

    public StorageNormsParser(ILogger<StorageNormsParser> logger, IConfigurationManager configurationManager,
        IPluginsManager pluginsManager, IDeserializer deserializer, ILogger<LocalFileSystemStorage> localStorageLogger,
        IStorageFilter storageFilter, INamespaceParser namespaceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _configurationManager = configurationManager;
        _pluginsManager = pluginsManager;
        _deserializer = deserializer;
        _localStorageLogger = localStorageLogger;
        _storageFilter = storageFilter;
        _namespaceParser = namespaceParser;
    }

    public StorageNormsInfo Parse(string path)
    {
        try
        {
            var segments = path.Split(ParserSeparator);
            if (segments.Length != 2)
            {
                return new StorageNormsInfo (new LocalFileSystemStorage(_localStorageLogger, _storageFilter), null, path);
            }

            var fileSystemExist = _configurationManager.IsExist(segments[0]);
            if (!fileSystemExist)
                throw new StorageNormsParserException(string.Format(Resources.StorageNormsParserStorageNotFoumd, segments[0]));

            var fileSystem = _configurationManager.GetSetting(segments[0]);

            if (_namespaceParser.Parse(fileSystem.Type) != PluginNamespace.Storage)
                throw new StorageNormsParserException(string.Format(Resources.StorageNormsParserInvalidStorageType, fileSystem.Type));

            var specifications = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (fileSystem.Specifications != null)
                specifications = fileSystem.Specifications;
            
            return new StorageNormsInfo(_pluginsManager.GetPlugin(fileSystem.Type).CastTo<IStoragePlugin>(), specifications, segments[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }
    
    public void Dispose() { }
}