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
    public override Namespace Namespace => Namespace.Connectors;
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

    public async Task Create(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Create(context, cancellationToken).ConfigureAwait(false);

    public async Task Write(Context context, CancellationToken cancellationToken = default) => 
        await _manager.Write(context, cancellationToken).ConfigureAwait(false);

    public async Task<InterchangeData> Read(Context context, CancellationToken cancellationToken = default) => 
        await _manager.Read(context,cancellationToken).ConfigureAwait(false);

    public async Task Update(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Update(context, cancellationToken).ConfigureAwait (false);

    public async Task Delete(Context context, CancellationToken cancellationToken = default) => 
        await _manager.Delete(context, cancellationToken).ConfigureAwait(false);

    public async Task<bool> Exist(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Exist(context, cancellationToken).ConfigureAwait(false);

    public async Task<InterchangeData> List(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntities(context, cancellationToken);

    public async Task Transfer(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    //public async Task Transfer(Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.Transfer(Namespace, Type, sourceContext, destinationContext, transferKind, 
    //        cancellationToken).ConfigureAwait(false);

    //public async Task ProcessTransfer(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransfer(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}