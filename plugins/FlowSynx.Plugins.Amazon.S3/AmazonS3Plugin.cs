using FlowSynx.PluginCore;
using FlowSynx.Plugins.Amazon.S3.Models;
using FlowSynx.Plugins.Amazon.S3.Services;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Extensions;

namespace FlowSynx.Plugins.Amazon.S3;

public class AmazonS3Plugin : Plugin
{
    private readonly ILogger<AmazonS3Plugin> _logger;
    private readonly IAmazonS3Connection _connection;
    private IAmazonS3Manager _manager = null!;
    private AmazonS3Specifications _s3Specifications = null!;

    public AmazonS3Plugin(ILogger<AmazonS3Plugin> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _connection = new AmazonS3Connection();
    }

    public override Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public override string Name => "Amazon.S3";
    public override string? Description => Resources.ConnectorDescription;
    public override PluginVersion Version => new PluginVersion(1, 0, 0);
    public override PluginNamespace Namespace => PluginNamespace.Connectors;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AmazonS3Specifications);

    public override Task Initialize()
    {
        _s3Specifications = Specifications.ToObject<AmazonS3Specifications>();
        var client = _connection.Connect(_s3Specifications);
        _manager = new AmazonS3Manager(_logger, client, _s3Specifications.Bucket);
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
                throw new NotSupportedException($"Amazon S3 plugin: Operation '{operation}' is not supported.");
        }
    }
}