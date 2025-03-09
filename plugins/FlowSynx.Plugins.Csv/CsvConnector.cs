using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.Connectors.Stream.Csv.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;
using FlowSynx.Data;

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
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(CsvSpecifications);

    public override Task Initialize()
    {
        _csvStreamSpecifications = Specifications.ToObject<CsvSpecifications>();
        _manager = new CsvManager(_logger, _dataService, _deserializer, _csvStreamSpecifications);
        return Task.CompletedTask;
    }

    public async Task Create(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Create(context, cancellationToken).ConfigureAwait(false);

    public async Task Write(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.Write(context, cancellationToken).ConfigureAwait(false);

    //public async Task<InterchangeData> Read(Context context, 
    //    CancellationToken cancellationToken = default) =>
    //    await _manager.Read(context, cancellationToken).ConfigureAwait(false);

    public async Task Update(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Update(context, cancellationToken).ConfigureAwait(false);

    public async Task Delete(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Delete(context, cancellationToken).ConfigureAwait(false);

    public async Task<bool> Exist(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Exist(context, cancellationToken).ConfigureAwait(false);

    public async Task<InterchangeData> List(Context context, 
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _manager.FilteredEntities(context, cancellationToken);
        return filteredData;
    }

    public async Task Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}