using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Google.Cloud.Models;
using FlowSynx.Connectors.Storage.Google.Cloud.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Storage.Google.Cloud;

public class GoogleCloudConnector : Connector
{
    private readonly ILogger<GoogleCloudConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly IGoogleCloudConnection _connection;
    private IGoogleCloudManager _manager = null!;
    private GoogleCloudSpecifications _googleCloudSpecifications = null!;

    public GoogleCloudConnector(ILogger<GoogleCloudConnector> logger, IDataService dataService, 
        ISerializer serializer, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _connection = new GoogleCloudConnection(serializer);
    }

    public override Guid Id => Guid.Parse("d3c52770-f001-4ea3-93b7-f113a956a091");
    public override string Name => "Google.Cloud";
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(GoogleCloudSpecifications);

    public override Task Initialize()
    {
        _googleCloudSpecifications = Specifications.ToObject<GoogleCloudSpecifications>();
        var client = _connection.Connect(_googleCloudSpecifications);
        _manager = new GoogleCloudManager(_logger, client, _googleCloudSpecifications, _dataService, _deserializer);
        return Task.CompletedTask;
    }

    public async Task Create(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Create(context, cancellationToken).ConfigureAwait(false);

    public async Task Write(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Write(context, cancellationToken).ConfigureAwait(false);

    public async Task<InterchangeData> Read(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Read(context, cancellationToken).ConfigureAwait(false);

    public async Task Update(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Update(context, cancellationToken).ConfigureAwait(false);

    public async Task Delete(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Delete(context, cancellationToken).ConfigureAwait(false);

    public async Task<bool> Exist(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Exist(context, cancellationToken);

    public async Task<InterchangeData> List(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntities(context, cancellationToken);

    public async Task Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}