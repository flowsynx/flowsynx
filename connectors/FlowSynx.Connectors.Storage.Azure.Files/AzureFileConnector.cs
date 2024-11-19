using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Storage.Azure.Files.Models;
using FlowSynx.Connectors.Storage.Azure.Files.Services;
using FlowSynx.Data.Queries;

namespace FlowSynx.Connectors.Storage.Azure.Files;

public class AzureFileConnector : Connector
{
    private readonly ILogger<AzureFileConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly IAzureFilesConnection _connection;
    private IAzureFilesManager _manager = null!;
    private AzureFilesSpecifications? _azureFilesSpecifications;

    public AzureFileConnector(ILogger<AzureFileConnector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _connection = new AzureFilesConnection();
    }

    public override Guid Id => Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d");
    public override string Name => "Azure.Files";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureFilesSpecifications);

    public override Task Initialize()
    {
        _azureFilesSpecifications = Specifications.ToObject<AzureFilesSpecifications>();
        var client = _connection.Connect(_azureFilesSpecifications);
        _manager = new AzureFilesManager(_logger, client, _dataService, _deserializer);
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
        await _manager.ExistAsync(context, cancellationToken);

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);

    public override async Task TransferAsync(Context sourceContext, 
        Context destinationContext, CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, cancellationToken).ConfigureAwait(false);

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default) =>
        await _manager.ProcessTransferAsync(context, transferData, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}