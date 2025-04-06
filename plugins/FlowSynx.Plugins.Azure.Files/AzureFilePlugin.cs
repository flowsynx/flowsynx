using FlowSynx.PluginCore;
using FlowSynx.Plugins.Azure.Files.Models;
using FlowSynx.Plugins.Azure.Files.Services;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Extensions;

namespace FlowSynx.Plugins.Azure.Files;

public class AzureFilePlugin : IPlugin
{
    private readonly ILogger<AzureFilePlugin> _logger;
    private readonly IAzureFilesConnection _connection;
    private IAzureFilesManager _manager = null!;
    private AzureFilesSpecifications? _azureFilesSpecifications;

    public AzureFilePlugin(ILogger<AzureFilePlugin> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _connection = new AzureFilesConnection();
    }

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d"),
                Name = "Azure.Files",
                Description = Resources.ConnectorDescription,
                Version = new PluginVersion(1, 0, 0),
                Namespace = PluginNamespace.Connectors,
                Author = "FlowSynx LLC."
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(AzureFilesSpecifications);
           
    public Task Initialize()
    {
        _azureFilesSpecifications = Specifications.ToObject<AzureFilesSpecifications>();
        var client = _connection.Connect(_azureFilesSpecifications);
        _manager = new AzureFilesManager(_logger, client);
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        switch (operation.ToLower())
        {
            case "create":
                await _manager.Create(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "delete":
                await _manager.Delete(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "exist":
                return await _manager.Exist(parameters, cancellationToken).ConfigureAwait(false);
            case "list":
                return await _manager.List(parameters, cancellationToken).ConfigureAwait(false);
            case "purge":
                await _manager.Purge(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "read":
                return await _manager.Read(parameters, cancellationToken).ConfigureAwait(false);
            case "write":
                await _manager.Write(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            default:
                throw new NotSupportedException($"Microsoft Azure Files plugin: Operation '{operation}' is not supported.");
        }
    }
}