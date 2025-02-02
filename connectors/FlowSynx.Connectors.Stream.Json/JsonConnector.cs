using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Stream.Json.Services;
using FlowSynx.Data.Queries;
using FlowSynx.Data.Extensions;
using FlowSynx.Data;

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
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(JsonSpecifications);

    public override Task Initialize()
    {
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
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _manager.FilteredEntities(context, cancellationToken);
        return filteredData;
    }

    public async Task Transfer(Context context,CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    public async Task<IEnumerable<CompressEntry>> Compress(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}