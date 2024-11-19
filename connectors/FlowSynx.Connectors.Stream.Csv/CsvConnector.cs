using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.Connectors.Stream.Csv.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Stream.Csv;

public class CsvConnector : Connector
{
    private readonly ILogger _logger;
    private CsvSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;
    private readonly IDataService _dataService;
    private ICsvManager _manager = null!;

    public CsvConnector(ILogger<CsvConnector> logger, IDataService dataService, IDeserializer deserializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _dataService = dataService;
    }

    public override Guid Id => Guid.Parse("ce2fc15b-cd5e-4eb0-a5b4-22fa714e5cc9");
    public override string Name => "CSV";
    public override Namespace Namespace => Namespace.Stream;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(CsvSpecifications);

    public override Task Initialize()
    {
        _csvStreamSpecifications = Specifications.ToObject<CsvSpecifications>();
        _manager = new CsvManager(_logger, _dataService, _deserializer, _csvStreamSpecifications);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.About(context, cancellationToken).ConfigureAwait(false);

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CreateAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task WriteAsync(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.WriteAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ReadAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.UpdateAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.DeleteAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ExistAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _manager.FilteredEntitiesAsync(context, cancellationToken);
        return filteredData.DataTableToList();
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, cancellationToken).ConfigureAwait(false);

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default) =>
        await _manager.ProcessTransferAsync(context, transferData, cancellationToken);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}