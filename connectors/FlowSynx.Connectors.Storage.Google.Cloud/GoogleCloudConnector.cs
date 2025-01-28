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
    public override Namespace Namespace => Namespace.Storage;
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

    public override async Task<object> About(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.About(context, cancellationToken).ConfigureAwait(false);

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CreateAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task WriteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.WriteAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<InterchangeData> ReadAsync(Context context, 
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
        await _manager.ExistAsync(context, cancellationToken);

    public override async Task<InterchangeData> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context, cancellationToken);

    public override async Task TransferAsync(Context context, CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(context, cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransferAsync(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransferAsync(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}