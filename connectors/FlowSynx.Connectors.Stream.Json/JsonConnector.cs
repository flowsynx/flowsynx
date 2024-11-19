using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Stream.Json.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Stream.Json;

public class JsonConnector : Connector
{
    private readonly IJsonManager _manager;

    public JsonConnector(ILogger<JsonConnector> logger, IDataService dataService,
        IDeserializer deserializer, ISerializer serializer)
    {
        _manager = new JsonManager(logger, dataService, deserializer, serializer);
    }

    public override Guid Id => Guid.Parse("0914e754-b203-4f37-9ac2-c67d86400eb9");
    public override string Name => "Json";
    public override Namespace Namespace => Namespace.Stream;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(JsonSpecifications);

    public override Task Initialize()
    {
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
        await _manager.ProcessTransferAsync(context, transferData, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}