using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Data.Filter;
using FlowSynx.Connectors.Storage.Google.Drive.Models;
using FlowSynx.Connectors.Storage.Google.Drive.Services;

namespace FlowSynx.Connectors.Storage.Google.Drive;

public class GoogleDriveConnector : Connector
{
    private readonly ILogger<GoogleDriveConnector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly IGoogleDriveConnection _connection;
    private IGoogleDriveManager _manager = null!;
    private GoogleDriveSpecifications _googleDriveSpecifications = null!;

    public GoogleDriveConnector(ILogger<GoogleDriveConnector> logger, IDataFilter dataFilter, 
        ISerializer serializer, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _connection = new GoogleDriveConnection(serializer);
    }

    public override Guid Id => Guid.Parse("359e62f0-8ccf-41c4-a1f5-4e34d6790e84");
    public override string Name => "Google.Drive";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(GoogleDriveSpecifications);

    public override Task Initialize()
    {
        _googleDriveSpecifications = Specifications.ToObject<GoogleDriveSpecifications>();
        var client = _connection.Connect(_googleDriveSpecifications);
        _manager = new GoogleDriveManager(_logger, client, _googleDriveSpecifications, _dataFilter, _deserializer);
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
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context, cancellationToken);

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