using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;
using FlowSynx.Connectors.Storage.Azure.Blobs.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Storage.Azure.Blobs;

public class AzureBlobConnector : Connector
{
    private readonly ILogger<AzureBlobConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly IAzureBlobConnection _connection;
    private IAzureBlobManager _manager = null!;
    private AzureBlobSpecifications _azureBlobSpecifications = null!;

    public AzureBlobConnector(ILogger<AzureBlobConnector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _connection = new AzureBlobConnection();
    }

    public override Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public override string Name => "Azure.Blobs";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureBlobSpecifications);

    public override Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobSpecifications>();
        var client = _connection.Connect(_azureBlobSpecifications);
        _manager = new AzureBlobManager(_logger, client, _dataService, _deserializer);
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
        await _manager.ExistAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<InterchangeData> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context, cancellationToken);

    public override async Task TransferAsync(Context context, CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(context, cancellationToken);

    //public override async Task TransferAsync(Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, transferKind, cancellationToken);

    //public override async Task ProcessTransferAsync(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransferAsync(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}