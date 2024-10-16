using EnsureThat;
using FlowSynx.Configuration;
using FlowSynx.Core.Exceptions;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.IO.Cache;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.LocalFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Manager;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Parers.Contex;

internal class ContextParser : IContextParser
{
    private readonly ILogger<ContextParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IConnectorsManager _connectorsManager;
    private readonly ILogger<LocalFileSystemConnector> _localStorageLogger;
    private readonly INamespaceParser _namespaceParser;
    private readonly ICache<string, Connector> _cache;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private const string ParserSeparator = "://";
    private const string PipelineSeparator = "|";

    public ContextParser(ILogger<ContextParser> logger, IConfigurationManager configurationManager,
        IConnectorsManager connectorsManager, ILogger<LocalFileSystemConnector> localStorageLogger,
        INamespaceParser namespaceParser, ICache<string, Connector> cache, 
        ISerializer serializer, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(connectorsManager, nameof(connectorsManager));
        EnsureArg.IsNotNull(localStorageLogger, nameof(localStorageLogger));
        EnsureArg.IsNotNull(namespaceParser, nameof(namespaceParser));
        EnsureArg.IsNotNull(cache, nameof(cache));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;
        _configurationManager = configurationManager;
        _connectorsManager = connectorsManager;
        _localStorageLogger = localStorageLogger;
        _namespaceParser = namespaceParser;
        _cache = cache;
        _serializer = serializer;
        _serviceProvider = serviceProvider;
    }

    public ConnectorContext Parse(string path)
    {
        try
        {
            var entity = string.Empty;
            var segments = path.Split(ParserSeparator);

            if (segments.Length != 2)
            {
                var localFileSystemConnector = GetConnector(LocalFileSystemConnector(), "LocalFileSystem", null);
                return CreateConnectorContext(localFileSystemConnector, path, null);
            }

            entity = segments[1];
            var pipelineSegments = segments[0].Split(PipelineSeparator);
            if (pipelineSegments.Length > 2)
                throw new StorageNormsParserException("Pipeline is not applied correctly.");

            string currentConfigName = string.Empty;
            string nextConfigName = string.Empty;
            if (pipelineSegments.Length == 1)
            {
                currentConfigName = pipelineSegments[0];
            }
            else
            {
                currentConfigName = pipelineSegments[0];
                nextConfigName = pipelineSegments[1];
            }

            var primaryConfigNameExist = _configurationManager.IsExist(currentConfigName);

            if (!primaryConfigNameExist)
                throw new StorageNormsParserException($"{currentConfigName} is not exist.");

            var primaryConfig = _configurationManager.Get(currentConfigName);
            if (_namespaceParser.Parse(primaryConfig.Type) == FlowSynx.Connectors.Abstractions.Namespace.Stream)
            {
                if (!string.IsNullOrEmpty(nextConfigName))
                {
                    var secondaryConfigNameExist = _configurationManager.IsExist(nextConfigName);
                    if (!secondaryConfigNameExist)
                        throw new StorageNormsParserException($"{nextConfigName} is not exist.");
                }
            }

            Connector? nextConnector = null;
            if (!string.IsNullOrEmpty(nextConfigName))
            {
                var secondaryConfig = _configurationManager.Get(nextConfigName);
                var getNextConnector = _connectorsManager.Get(secondaryConfig.Type);
                nextConnector = GetConnector(getNextConnector, nextConfigName, secondaryConfig.Specifications.ToSpecifications());
            }

            var getCurrentConnector = _connectorsManager.Get(primaryConfig.Type);
            var currentConnector = GetConnector(getCurrentConnector, currentConfigName, primaryConfig.Specifications.ToSpecifications());

            return CreateConnectorContext(currentConnector, entity, nextConnector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }

    private LocalFileSystemConnector LocalFileSystemConnector()
    {
        var iConnectorsServices = _serviceProvider.GetServices<Connector>();
        var localFileSystem = iConnectorsServices.First(o => o.GetType() == typeof(LocalFileSystemConnector));
        return (LocalFileSystemConnector)localFileSystem;
    }

    private Connector GetConnector(Connector connector, string configName, Connectors.Abstractions.Specifications? specifications)
    {
        var key = GenerateKey(connector.Id, configName, specifications);
        var cachedConnectorContext = _cache.Get(key);

        if (cachedConnectorContext != null)
        {
            _logger.LogInformation($"{connector.Name} is found in Cache.");
            return cachedConnectorContext;
        }

        connector.Specifications = specifications;
        connector.Initialize();
        _cache.Set(key, connector);
        return connector;
    }

    private ConnectorContext CreateConnectorContext(Connector currentConnector, string entity, Connector? nextConnector)
    {
        var context = nextConnector is null ? new Context(entity) : new Context(entity, nextConnector);
        return new ConnectorContext(currentConnector, context);
    }

    private string GenerateKey(Guid connectorId, string configName, object? connectorSpecifications)
    {
        var key = $"{connectorId}-{configName}";
        var result = connectorSpecifications == null ? key : $"{key}-{_serializer.Serialize(connectorSpecifications)}";
        return Security.HashHelper.Md5.GetHash(result);
    }

    public void Dispose() { }
}