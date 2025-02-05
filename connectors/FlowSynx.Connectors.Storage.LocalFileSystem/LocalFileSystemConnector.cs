using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.LocalFileSystem.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data;
using FlowSynx.Abstractions;

namespace FlowSynx.Connectors.Storage.LocalFileSystem;

public class LocalFileSystemConnector : Connector
{
    private readonly ILogger<LocalFileSystemConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private ILocalFileManager _manager = null!;

    public LocalFileSystemConnector(ILogger<LocalFileSystemConnector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public override string Name => "LocalFileSystem";
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public override Task Initialize()
    {
        _manager = new LocalFileManager(_logger, _dataService, _deserializer);
        return Task.CompletedTask;
    }

    [ConnectorMetadata(LinkType.Output)]
    public async Task<Result<object>> About(Context context, CancellationToken cancellationToken = default) =>
        await _manager.About(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Input)]
    public async Task<Result> Create(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Create(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Input)]
    public async Task<Result> Write(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Write(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Output)]
    public async Task<Result<InterchangeData>> Read(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Read(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.InputOutput)]
    public async Task<Result> Rename(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Rename(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Output)]
    public async Task<Result> Delete(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Delete(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.InputOutput)]
    public async Task<Result<bool>> Exist(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Exist(context);

    [ConnectorMetadata(LinkType.InputOutput)]
    public async Task<Result<InterchangeData>> List(Context context, CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntities(context).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Input)]
    public async Task<Result> Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    [ConnectorMetadata(LinkType.Input)]
    public async Task<Result<IEnumerable<CompressEntry>>> Compress(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}