using EnsureThat;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Memory.Models;
using FlowSynx.Connectors.Storage.Memory.Services;
using MemoryMetrics = FlowSynx.Connectors.Storage.Memory.Services.MemoryMetrics;
using FlowSynx.Data.Queries;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Storage.Memory;

public class MemoryConnector : Connector
{
    private readonly ILogger<MemoryConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly IMemoryMetrics _memoryMetrics;
    private IMemoryManager _manager = null!;

    public MemoryConnector(ILogger<MemoryConnector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _memoryMetrics = new MemoryMetrics();
    }

    public override Guid Id => Guid.Parse("ac220180-021e-4150-b0e1-c4d4bdbfb9f0");
    public override string Name => "Memory";
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(MemoryStorageSpecifications);

    public override Task Initialize()
    {
        _manager = new MemoryManager(_logger, _dataService, _deserializer, _memoryMetrics);
        return Task.CompletedTask;
    }

    public async Task<object> About(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.About(context).ConfigureAwait(false);

    public async Task Create(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.Create(context).ConfigureAwait(false);

    public async Task Write(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Write(context).ConfigureAwait(false);

    public async Task<InterchangeData> Read(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Read(context).ConfigureAwait(false);

    public async Task Update(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Update(context).ConfigureAwait(false);

    public async Task Delete(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Delete(context).ConfigureAwait(false);

    public async Task<bool> Exist(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Exist(context).ConfigureAwait(false);

    public async Task<InterchangeData> List(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntities(context).ConfigureAwait(false);

    public async Task Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}