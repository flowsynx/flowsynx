using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using FlowSynx.Plugins.LocalFileSystem.Services;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugins.LocalFileSystem;

public class LocalFileSystemPlugin : Plugin
{
    private readonly ILogger<LocalFileSystemPlugin> _logger;
    private ILocalFileManager _manager = null!;

    public LocalFileSystemPlugin(ILogger<LocalFileSystemPlugin> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public override Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public override string Name => "LocalFileSystem";
    public override PluginNamespace Namespace => PluginNamespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public override Task Initialize()
    {
        _manager = new LocalFileManager(_logger);
        return Task.CompletedTask;
    }

    public override async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        switch (operation.ToLower())
        {
            case "create":
                await _manager.Create(parameters).ConfigureAwait(false);
                return null;
            case "delete":
                await _manager.Delete(parameters).ConfigureAwait(false);
                return null;
            case "exist":
                return await _manager.Exist(parameters).ConfigureAwait(false);
            case "list":
                return await _manager.List(parameters).ConfigureAwait(false);
            case "purge":
                await _manager.Purge(parameters).ConfigureAwait(false);
                return null;
            case "read":
                return await _manager.Read(parameters).ConfigureAwait(false);
            case "rename":
                await _manager.Rename(parameters).ConfigureAwait(false);
                return null;
            case "write":
                await _manager.Write(parameters).ConfigureAwait(false);
                return null;
            default:
                throw new NotSupportedException($"Local FileSystem plugin: Operation '{operation}' is not supported.");
        }
    }
}