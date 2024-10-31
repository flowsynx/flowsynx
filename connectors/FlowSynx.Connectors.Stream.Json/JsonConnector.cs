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
    private readonly IJsonManager _jsonManager;

    public JsonConnector(ILogger<JsonConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _jsonManager = new JsonManager(logger, dataFilter, deserializer, serializer);
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
        return _jsonManager.About(context, cancellationToken);
    }

    public override Task CreateAsync(Context context,
        CancellationToken cancellationToken = default)
    {
        return _jsonManager.CreateAsync(context, cancellationToken);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        await _jsonManager.WriteAsync(context, dataOptions, cancellationToken);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _jsonManager.ReadAsync(context, cancellationToken);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return _jsonManager.DeleteAsync(context, cancellationToken);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _jsonManager.ExistAsync(context, cancellationToken);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _jsonManager.FilteredEntitiesAsync(context, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext, 
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var transferData = await _jsonManager.PrepareDataForTransferring(Namespace, Type, sourceContext, cancellationToken);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        await _jsonManager.TransferData(context, transferData, cancellationToken);
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
        
        var filteredData = await _jsonManager.FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));
        
        if (compressOptions.SeparateJsonPerRow is false)
            return await _jsonManager.CompressDataTable(filteredData, indentedOptions.Indented);
        
        return await _jsonManager.CompressDataRows(filteredData.Rows, indentedOptions.Indented);
    }
}