using EnsureThat;
using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Configuration;
using FlowSync.Core.Parers.Namespace;
using FlowSync.Core.Parers.Norms.Storage;
using FlowSync.Core.Plugins;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.Exceptions;
using FlowSync.Infrastructure.Extensions;
using FlowSync.Storage.LocalFileSystem;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.Parers.Norms.Storage;

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
                return new StorageNormsInfo
                {
                    Name = "LocalFileSystem",
                    Plugin = new LocalFileSystemStorage(_localStorageLogger, _storageFilter),
                    Specifications = null,
                    Path = path
                };
            }

            var fileSystemExist = _configurationManager.IsExist(segments[0]);
            if (!fileSystemExist)
                throw new StorageNormsParserException(string.Format(FlowSyncInfrastructureResource.FileSystemRemotePathParserFileSystemNotFoumd, segments[0]));

            var fileSystem = _configurationManager.GetSetting(segments[0]);

            if (_namespaceParser.Parse(fileSystem.Type) != PluginNamespace.Storage)
                throw new StorageNormsParserException($"The selected plugin type '{fileSystem.Type}' is not valid storage plugin type.");

            var specifications = new Specifications();
            if (fileSystem.Specifications != null)
                specifications = _deserializer.Deserialize<Specifications>(fileSystem.Specifications.ToString());

            return new StorageNormsInfo
            {
                Name = fileSystem.Name,
                Plugin = _pluginsManager.GetPlugin(fileSystem.Type).CastTo<IStoragePlugin>(),
                Specifications = specifications,
                Path = segments[1]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public void Dispose() { }
}