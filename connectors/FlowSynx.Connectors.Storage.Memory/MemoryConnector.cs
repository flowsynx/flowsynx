using EnsureThat;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Memory.Models;
using FlowSynx.Connectors.Storage.Memory.Services;
using MemoryMetrics = FlowSynx.Connectors.Storage.Memory.Services.MemoryMetrics;
using FlowSynx.Data.Queries;

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
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(MemoryStorageSpecifications);

    public override Task Initialize()
    {
        _manager = new MemoryManager(_logger, _dataService, _deserializer, _memoryMetrics);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.About(context).ConfigureAwait(false);

    public override async Task CreateAsync(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.CreateAsync(context).ConfigureAwait(false);

    public override async Task WriteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.WriteAsync(context).ConfigureAwait(false);

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ReadAsync(context).ConfigureAwait(false);

    public override async Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.UpdateAsync(context).ConfigureAwait(false);

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.DeleteAsync(context).ConfigureAwait(false);

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ExistAsync(context).ConfigureAwait(false);

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context).ConfigureAwait(false);

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, cancellationToken).ConfigureAwait(false);

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default) =>
        await _manager.ProcessTransferAsync(context, transferData, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}