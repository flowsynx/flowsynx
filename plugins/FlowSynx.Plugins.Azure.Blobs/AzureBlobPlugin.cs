using FlowSynx.PluginCore;
using FlowSynx.Plugins.Azure.Blobs.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Azure.Blobs.Services;

namespace FlowSynx.Plugins.Azure.Blobs;

public class AzureBlobPlugin : IPlugin
{
    private IAzureBlobManager _manager = null!;
    private AzureBlobSpecifications _azureBlobSpecifications = null!;

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8"),
                Name = "Azure.Blobs",
                Description = Resources.ConnectorDescription,
                Version = new PluginVersion(1, 0, 0),
                Namespace = PluginNamespace.Connectors,
                Author = "FlowSynx LLC."
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(AzureBlobSpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var connection = new AzureBlobConnection();
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobSpecifications>();
        var client = connection.Connect(_azureBlobSpecifications);
        _manager = new AzureBlobManager(logger, client, _azureBlobSpecifications.ContainerName);
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
                throw new NotSupportedException($"Microsoft Azure Blobs plugin: Operation '{operation}' is not supported.");
        }
    }
}