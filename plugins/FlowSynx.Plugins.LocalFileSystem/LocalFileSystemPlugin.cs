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
            case "read":
                return await _manager.Read(parameters).ConfigureAwait(false);
            case "write":
                await _manager.Write(parameters).ConfigureAwait(false);
                return null;
            default:
                throw new NotSupportedException($"Local FileSystem plugin: Operation {operation} is not supported.");
        }
    }

    //public async Task<object> About(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.About(context).ConfigureAwait(false);

    //public async Task Create(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Create(context).ConfigureAwait(false);

    //public async Task Write(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Write(context).ConfigureAwait(false);

    //public async Task<string> Read(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Read(context).ConfigureAwait(false);

    //public async Task Rename(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Rename(context).ConfigureAwait(false);

    //public async Task Delete(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Delete(context).ConfigureAwait(false);

    //public async Task<bool> Exist(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Exist(context);

    //public async Task<InterchangeData> List(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.FilteredEntities(context).ConfigureAwait(false);

    //public async Task Transfer(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    ////public override async Task ProcessTransfer(Context context, TransferData transferData,
    ////    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    ////    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    //public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken = default) =>
    //    await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}