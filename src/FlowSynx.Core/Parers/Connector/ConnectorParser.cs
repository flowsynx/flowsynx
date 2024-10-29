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

namespace FlowSynx.Core.Parers.Connector;

internal class ConnectorParser : IConnectorParser
{
    private readonly ILogger<ConnectorParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IConnectorsManager _connectorsManager;
    private readonly ILogger<LocalFileSystemConnector> _localStorageLogger;
    private readonly INamespaceParser _namespaceParser;
    private readonly ICache<string, FlowSynx.Connectors.Abstractions.Connector> _cache;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private const string ParserSeparator = "|";

    public ConnectorParser(ILogger<ConnectorParser> logger, IConfigurationManager configurationManager,
        IConnectorsManager connectorsManager, ILogger<LocalFileSystemConnector> localStorageLogger,
        INamespaceParser namespaceParser, ICache<string, FlowSynx.Connectors.Abstractions.Connector> cache, 
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

    public ConnectorContext Parse(string? connectorInput)
    {
        try
        {
            if (string.IsNullOrEmpty(connectorInput))
            {
                var localFileSystemConnector = GetConnector(LocalFileSystemConnector(), "LocalFileSystem", null);
                return CreateConnectorContext(localFileSystemConnector, null);
            }

            var connectors = connectorInput.Split(ParserSeparator);
            if (connectors.Length > 2)
                throw new StorageNormsParserException("Pipeline is not applied correctly.");

            string currentConfigName;
            string nextConfigName = string.Empty;
            if (connectors.Length == 1)
            {
                currentConfigName = connectors[0];
            }
            else
            {
                currentConfigName = connectors[0];
                nextConfigName = connectors[1];
            }

            var currentConfigExist = _configurationManager.IsExist(currentConfigName);

            if (!currentConfigExist)
                throw new StorageNormsParserException($"{currentConfigName} is not exist.");

            var currentConfig = _configurationManager.Get(currentConfigName);
            if (_namespaceParser.Parse(currentConfig.Type) == FlowSynx.Connectors.Abstractions.Namespace.Stream)
            {
                if (!string.IsNullOrEmpty(nextConfigName))
                {
                    var secondaryConfigNameExist = _configurationManager.IsExist(nextConfigName);
                    if (!secondaryConfigNameExist)
                        throw new StorageNormsParserException($"{nextConfigName} is not exist.");
                }
            }

            FlowSynx.Connectors.Abstractions.Connector? nextConnector = null;
            if (!string.IsNullOrEmpty(nextConfigName))
            {
                var secondaryConfig = _configurationManager.Get(nextConfigName);
                var getNextConnector = _connectorsManager.Get(secondaryConfig.Type);
                nextConnector = GetConnector(getNextConnector, nextConfigName, secondaryConfig.Specifications.ToSpecifications());
            }

            var getCurrentConnector = _connectorsManager.Get(currentConfig.Type);
            var currentConnector = GetConnector(getCurrentConnector, currentConfigName, currentConfig.Specifications.ToSpecifications());

            return CreateConnectorContext(currentConnector, nextConnector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new StorageNormsParserException(ex.Message);
        }
    }

    private LocalFileSystemConnector LocalFileSystemConnector()
    {
        var iConnectorsServices = _serviceProvider.GetServices<FlowSynx.Connectors.Abstractions.Connector>();
        var localFileSystem = iConnectorsServices.First(o => o.GetType() == typeof(LocalFileSystemConnector));
        return (LocalFileSystemConnector)localFileSystem;
    }

    private FlowSynx.Connectors.Abstractions.Connector GetConnector(FlowSynx.Connectors.Abstractions.Connector connector, string configName, Connectors.Abstractions.Specifications? specifications)
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

    private ConnectorContext CreateConnectorContext(FlowSynx.Connectors.Abstractions.Connector currentConnector, 
        FlowSynx.Connectors.Abstractions.Connector? nextConnector)
    {
        var nextConnectorContext = nextConnector is null ? null : new ConnectorContext(nextConnector);
        return new ConnectorContext(currentConnector)
        {
            Next = nextConnectorContext
        };
    }

    private string GenerateKey(Guid connectorId, string configName, object? connectorSpecifications)
    {
        var key = $"{connectorId}-{configName}";
        var result = connectorSpecifications == null ? key : $"{key}-{_serializer.Serialize(connectorSpecifications)}";
        return Security.HashHelper.Md5.GetHash(result);
    }

    public void Dispose() { }
}