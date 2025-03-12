//using EnsureThat;
//using FlowSynx.Configuration;
//using FlowSynx.Application.Exceptions;
//using FlowSynx.Application.Parers.Namespace;
//using FlowSynx.IO.Cache;
//using FlowSynx.Connectors.Abstractions.Extensions;
//using FlowSynx.Connectors.Storage.LocalFileSystem;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using FlowSynx.Connectors.Manager;
//using FlowSynx.Connectors.Abstractions;
//using FlowSynx.Application.Services;

//namespace FlowSynx.Application.Parers.Connector;

//internal class ConnectorParser : IConnectorParser
//{
//    private readonly ILogger<ConnectorParser> _logger;
//    private readonly IConfigurationManager _configurationManager;
//    private readonly IConnectorsManager _connectorsManager;
//    private readonly ILogger<LocalFileSystemConnector> _localStorageLogger;
//    private readonly INamespaceParser _namespaceParser;
//    private readonly ICache<string, FlowSynx.Connectors.Abstractions.Connector> _cache;
//    private readonly IJsonSerializer _serializer;
//    private readonly IJsonDeserializer _deserializer;
//    private readonly IServiceProvider _serviceProvider;
//    private const string ParserSeparator = "|";

//    public ConnectorParser(ILogger<ConnectorParser> logger, IConfigurationManager configurationManager,
//        IConnectorsManager connectorsManager, ILogger<LocalFileSystemConnector> localStorageLogger,
//        INamespaceParser namespaceParser, ICache<string, FlowSynx.Connectors.Abstractions.Connector> cache, 
//        IJsonSerializer serializer, IJsonDeserializer deserializer, IServiceProvider serviceProvider)
//    {
//        EnsureArg.IsNotNull(logger, nameof(logger));
//        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
//        EnsureArg.IsNotNull(connectorsManager, nameof(connectorsManager));
//        EnsureArg.IsNotNull(localStorageLogger, nameof(localStorageLogger));
//        EnsureArg.IsNotNull(namespaceParser, nameof(namespaceParser));
//        EnsureArg.IsNotNull(cache, nameof(cache));
//        EnsureArg.IsNotNull(serializer, nameof(serializer));
//        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
//        _logger = logger;
//        _configurationManager = configurationManager;
//        _connectorsManager = connectorsManager;
//        _localStorageLogger = localStorageLogger;
//        _namespaceParser = namespaceParser;
//        _cache = cache;
//        _serializer = serializer;
//        _deserializer = deserializer;
//        _serviceProvider = serviceProvider;
//    }

//    public FlowSynx.Connectors.Abstractions.Connector Parse(object? type)
//    {
//        try
//        {
//            return type is string 
//                ? GetConnectorBasedOnName(type.ToString()) 
//                : GetConnectorBasedOnType(type);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex.Message);
//            throw new StorageNormsParserException(ex.Message);
//        }
//    }

//    private FlowSynx.Connectors.Abstractions.Connector GetConnectorBasedOnName(string? configName)
//    {
//        try
//        {
//            if (string.IsNullOrEmpty(configName))
//            {
//                var localFileSystemConnector = GetConnector(LocalFileSystemConnector(), "LocalFileSystem", null);
//                return localFileSystemConnector;
//            }

//            var currentConfigExist = _configurationManager.IsExist(configName);

//            if (!currentConfigExist)
//                throw new StorageNormsParserException($"{configName} is not exist.");

//            var currentConfig = _configurationManager.Get(configName);
//            var getCurrentConnector = _connectorsManager.Get(currentConfig.Type);
//            var currentConnector = GetConnector(getCurrentConnector, configName, currentConfig.Specifications.ToSpecifications());

//            return currentConnector;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex.Message);
//            throw new StorageNormsParserException(ex.Message);
//        }
//    }

//    private FlowSynx.Connectors.Abstractions.Connector GetConnectorBasedOnType(object? type)
//    {
//        try
//        {
//            string? connectorType;
//            Connectors.Abstractions.Specifications? specifications = null;

//            if (type is null)
//            {
//                connectorType = string.Empty;
//            }
//            else
//            {
//                var conn = _deserializer.Deserialize<TypeConfiguration>(type.ToString());
//                connectorType = conn.Connector;
//                specifications = conn.Specifications;
//            }

//            if (string.IsNullOrEmpty(connectorType))
//            {
//                var localFileSystemConnector = GetConnector(LocalFileSystemConnector(), "LocalFileSystem", specifications);
//                return localFileSystemConnector;
//            }
            
//            var getCurrentConnector = _connectorsManager.Get(connectorType);
//            var currentConnector = GetConnector(getCurrentConnector, connectorType, specifications);

//            return currentConnector;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex.Message);
//            throw new StorageNormsParserException(ex.Message);
//        }
//    }

//    private LocalFileSystemConnector LocalFileSystemConnector()
//    {
//        var iConnectorsServices = _serviceProvider.GetServices<FlowSynx.Connectors.Abstractions.Connector>();
//        var localFileSystem = iConnectorsServices.First(o => o.GetType() == typeof(LocalFileSystemConnector));
//        return (LocalFileSystemConnector)localFileSystem;
//    }

//    private FlowSynx.Connectors.Abstractions.Connector GetConnector(FlowSynx.Connectors.Abstractions.Connector connector, 
//        string configName, Connectors.Abstractions.Specifications? specifications)
//    {
//        var key = GenerateKey(connector.Id, configName, specifications);
//        var cachedConnectorContext = _cache.Get(key);

//        if (cachedConnectorContext != null)
//        {
//            _logger.LogInformation($"{connector.Name} is found in Cache.");
//            return cachedConnectorContext;
//        }

//        connector.Specifications = specifications;
//        connector.Initialize();
//        _cache.Set(key, connector);
//        return connector;
//    }

//    private string GenerateKey(Guid connectorId, string configName, object? connectorSpecifications)
//    {
//        var key = $"{connectorId}-{configName}";
//        var result = connectorSpecifications == null ? key : $"{key}-{_serializer.Serialize(connectorSpecifications)}";
//        return Security.HashHelper.Md5.GetHash(result);
//    }

//    public void Dispose() { }
//}

//public class TypeConfiguration
//{
//    public string? Connector { get; set; }
//    public Connectors.Abstractions.Specifications? Specifications { get; set; }
//}