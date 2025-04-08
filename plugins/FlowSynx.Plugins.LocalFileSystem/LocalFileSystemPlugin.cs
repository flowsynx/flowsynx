using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using FlowSynx.Plugins.LocalFileSystem.Services;

namespace FlowSynx.Plugins.LocalFileSystem;

public class LocalFileSystemPlugin : IPlugin
{
    private ILocalFileManager _manager = null!;

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("f6304870-0294-453e-9598-a82167ace653"),
                Name = "LocalFileSystem",
                Description = Resources.ConnectorDescription,
                Version = new PluginVersion(1, 0, 0),
                Namespace = PluginNamespace.Connectors,
                Author = "FlowSynx LLC."
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _manager = new LocalFileManager(logger);
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
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