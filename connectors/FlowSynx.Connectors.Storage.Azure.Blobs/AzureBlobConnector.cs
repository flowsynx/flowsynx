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
    public override Namespace Namespace => Namespace.Connectors;
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
        await _manager.Exist(context, cancellationToken).ConfigureAwait(false);

    public async Task<InterchangeData> List(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntities(context, cancellationToken);

    public async Task Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken);

    //public override async Task Transfer(Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.Transfer(Namespace, Type, sourceContext, destinationContext, transferKind, cancellationToken);

    //public override async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}