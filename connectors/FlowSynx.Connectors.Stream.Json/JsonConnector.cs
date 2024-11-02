using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Stream.Exceptions;
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Stream.Json.Services;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Connectors.Stream.Json;

public class JsonConnector : Connector
{
    private readonly IJsonManager _manager;

    public JsonConnector(ILogger<JsonConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _manager = new JsonManager(logger, dataFilter, deserializer, serializer);
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

    public override Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        return _manager.About(context, cancellationToken);
    }

    public override Task CreateAsync(Context context,
        CancellationToken cancellationToken = default)
    {
        return _manager.CreateAsync(context, cancellationToken);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        await _manager.WriteAsync(context, dataOptions, cancellationToken);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _manager.ReadAsync(context, cancellationToken);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return _manager.DeleteAsync(context, cancellationToken);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _manager.ExistAsync(context, cancellationToken);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _manager.FilteredEntitiesAsync(context, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext, 
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var transferData = await _manager.PrepareDataForTransferring(Namespace, Type, sourceContext, cancellationToken);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        await _manager.TransferData(context, transferData, cancellationToken);
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);
        
        var compressOptions = context.Options.ToObject<CompressOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();
        
        var filteredData = await _manager.FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));
        
        if (compressOptions.SeparateJsonPerRow is false)
            return await _manager.CompressDataTable(filteredData, indentedOptions.Indented);
        
        return await _manager.CompressDataRows(filteredData.Rows, indentedOptions.Indented);
    }
}