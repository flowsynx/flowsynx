using FlowSynx.PluginCore;
using FlowSynx.Plugins.Azure.Blobs.Models;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Azure.Blobs.Services;

namespace FlowSynx.Plugins.Azure.Blobs;

public class AzureBlobPlugin : Plugin
{
    private readonly ILogger<AzureBlobPlugin> _logger;
    private readonly IAzureBlobConnection _connection;
    private IAzureBlobManager _manager = null!;
    private AzureBlobSpecifications _azureBlobSpecifications = null!;

    public AzureBlobPlugin(ILogger<AzureBlobPlugin> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _connection = new AzureBlobConnection();
    }

    public override Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public override string Name => "Azure.Blobs";
    public override string? Description => Resources.ConnectorDescription;
    public override PluginVersion Version => new PluginVersion(1, 0, 0);
    public override PluginNamespace Namespace => PluginNamespace.Connectors;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureBlobSpecifications);

    public override Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobSpecifications>();
        var client = _connection.Connect(_azureBlobSpecifications);
        _manager = new AzureBlobManager(_logger, client, _azureBlobSpecifications.ContainerName);
        return Task.CompletedTask;
    }

    public override async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
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
                throw new NotSupportedException($"Microsoft Azure Blobs plugin: Operation '{operation}' is not supported.");
        }
    }
}