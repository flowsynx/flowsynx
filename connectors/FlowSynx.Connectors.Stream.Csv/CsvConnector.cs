using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Data.Extensions;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.Connectors.Stream.Csv.Services;

namespace FlowSynx.Connectors.Stream.Csv;

public class CsvConnector : Connector
{
    private readonly ILogger _logger;
    private CsvSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private ICsvManager _csvManager = null!;

    public CsvConnector(ILogger<CsvConnector> logger, IDataFilter dataFilter, IDeserializer deserializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _dataFilter = dataFilter;
    }

    public override Guid Id => Guid.Parse("ce2fc15b-cd5e-4eb0-a5b4-22fa714e5cc9");
    public override string Name => "CSV";
    public override Namespace Namespace => Namespace.Stream;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(CsvSpecifications);

    public override Task Initialize()
    {
        _csvStreamSpecifications = Specifications.ToObject<CsvSpecifications>();
        _csvManager = new CsvManager(_logger, _dataFilter, _deserializer, _csvStreamSpecifications);
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        return _csvManager.About(context, cancellationToken);
    }

    public override Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return _csvManager.CreateAsync(context, cancellationToken);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        await _csvManager.WriteAsync(context, dataOptions, cancellationToken);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _csvManager.ReadAsync(context, cancellationToken);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        await _csvManager.DeleteAsync(context, cancellationToken);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        return await _csvManager.ExistAsync(context, cancellationToken);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var filteredData = await _csvManager.FilteredEntitiesAsync(context, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var transferData = await _csvManager.PrepareDataForTransferring(Namespace, Type, sourceContext, cancellationToken);
        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        await _csvManager.TransferData(context, transferData, cancellationToken);
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

        if (!_csvManager.IsCsvFile(path))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var filteredData = await _csvManager.FilteredEntitiesAsync(context, cancellationToken);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressOptions = context.Options.ToObject<CompressOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        if (compressOptions.SeparateCsvPerRow is false)
            return await _csvManager.CompressDataTable(filteredData, delimiterOptions);

        return await _csvManager.CompressDataRows(filteredData.Rows, delimiterOptions);
    }
}