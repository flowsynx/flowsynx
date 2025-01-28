using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Storage.Amazon.S3.Services;
using FlowSynx.Connectors.Storage.Amazon.S3.Models;
using FlowSynx.Data.Queries;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Storage.Amazon.S3;

public class AmazonS3Connector : Connector
{
    private readonly ILogger<AmazonS3Connector> _logger;
    private readonly IDeserializer _deserializer;
    private readonly IDataService _dataService;
    private readonly IAmazonS3Connection _connection;
    private IAmazonS3Manager _manager = null!;
    private AmazonS3Specifications _s3Specifications = null!;

    public AmazonS3Connector(ILogger<AmazonS3Connector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _connection = new AmazonS3Connection();
    }

    public override Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public override string Name => "Amazon.S3";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AmazonS3Specifications);

    public override Task Initialize()
    {
        _s3Specifications = Specifications.ToObject<AmazonS3Specifications>();
        var client = _connection.Connect(_s3Specifications);
        _manager = new AmazonS3Manager(_logger, client, _dataService, _deserializer);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, CancellationToken cancellationToken = default) => 
        await _manager.About(context, cancellationToken).ConfigureAwait(false);

    public override async Task CreateAsync(Context context, CancellationToken cancellationToken = default) =>
        await _manager.CreateAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task WriteAsync(Context context, CancellationToken cancellationToken = default) => 
        await _manager.WriteAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<InterchangeData> ReadAsync(Context context, CancellationToken cancellationToken = default) => 
        await _manager.ReadAsync(context,cancellationToken).ConfigureAwait(false);

    public override async Task UpdateAsync(Context context, CancellationToken cancellationToken = default) =>
        await _manager.UpdateAsync(context, cancellationToken).ConfigureAwait (false);

    public override async Task DeleteAsync(Context context, CancellationToken cancellationToken = default) => 
        await _manager.DeleteAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ExistAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task<InterchangeData> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context, cancellationToken);

    public override async Task TransferAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(context, cancellationToken).ConfigureAwait(false);

    //public override async Task TransferAsync(Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, transferKind, 
    //        cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransferAsync(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransferAsync(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}