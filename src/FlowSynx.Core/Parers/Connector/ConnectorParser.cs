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

    public FlowSynx.Connectors.Abstractions.Connector Parse(string? connector)
    {
        try
        {
            if (string.IsNullOrEmpty(connector))
            {
                var localFileSystemConnector = GetConnector(LocalFileSystemConnector(), "LocalFileSystem", null);
                return localFileSystemConnector;
            }

            var currentConfigExist = _configurationManager.IsExist(connector);

            if (!currentConfigExist)
                throw new StorageNormsParserException($"{connector} is not exist.");

            var currentConfig = _configurationManager.Get(connector);
            var getCurrentConnector = _connectorsManager.Get(currentConfig.Type);
            var currentConnector = GetConnector(getCurrentConnector, connector, currentConfig.Specifications.ToSpecifications());

            return currentConnector;
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

    private FlowSynx.Connectors.Abstractions.Connector GetConnector(FlowSynx.Connectors.Abstractions.Connector connector, 
        string configName, Connectors.Abstractions.Specifications? specifications)
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

    private string GenerateKey(Guid connectorId, string configName, object? connectorSpecifications)
    {
        var key = $"{connectorId}-{configName}";
        var result = connectorSpecifications == null ? key : $"{key}-{_serializer.Serialize(connectorSpecifications)}";
        return Security.HashHelper.Md5.GetHash(result);
    }

    public void Dispose() { }
}