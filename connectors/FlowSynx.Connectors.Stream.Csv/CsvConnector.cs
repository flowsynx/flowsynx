using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Data.Extensions;
using System.Text;
using System.Data;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.Connectors.Stream.Csv.Services;

namespace FlowSynx.Connectors.Stream.Csv;

public class CsvConnector : Connector
{
    private readonly ILogger _logger;
    private CsvSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private readonly ICsvManager _csvManager;

    public CsvConnector(ILogger<CsvConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _dataFilter = dataFilter;
        _csvManager = new CsvManager(logger, serializer);
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
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.CreateOperrationNotSupported);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var content = PrepareDataForWrite(writeOptions, delimiterOptions, dataOptions);
        if (context.ConnectorContext?.Current != null)
        {
            var newContext = new Context(context.Options, context.ConnectorContext.Next);
            await context.ConnectorContext.Current.WriteAsync(newContext, content, cancellationToken);
            return;
        }

        var append = writeOptions.OverWrite is false;
        await WriteEntityAsync(pathOptions.Path, content, append, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var readOptions = context.Options.ToObject<ReadOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var content = await ReadContent(context, cancellationToken);

        return await ReadEntityAsync(content, readOptions, listOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var content = await ReadContent(context, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(content, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        _csvManager.Delete(dataTable, filteredData);

        var data = _csvManager.ToCsv(dataTable, delimiter);

        await WriteContent(context, data, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var content = await ReadContent(context, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);

        return filteredData.Rows.Count > 0;
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var content = await ReadContent(context, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var transferData = await PrepareTransferring(sourceContext, cancellationToken);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var transferOptions = context.Options.ToObject<TransferOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataTable = new DataTable();

        foreach (var column in transferData.Columns)
            dataTable.Columns.Add(column);

        if (transferOptions.SeparateCsvPerRow is true)
        {
            if (!PathHelper.IsDirectory(path))
                throw new StreamException(Resources.ThePathIsNotDirectory);

            foreach (var row in transferData.Rows)
            {
                if (row.Items != null)
                {
                    var newRow = dataTable.NewRow();
                    newRow.ItemArray = row.Items;
                    dataTable.Rows.Add(newRow);

                    var data = _csvManager.ToCsv(newRow, transferData.Columns.ToArray(), delimiter);
                    var newPath = transferData.Namespace == Namespace.Storage
                        ? row.Key
                        : PathHelper.Combine(path, row.Key);

                    if (Path.GetExtension(newPath) != _csvManager.Extension)
                    {
                        _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                           $"So its extension will be automatically changed to {_csvManager.Extension}");

                        newPath = Path.ChangeExtension(path, _csvManager.Extension);
                    }

                    var clonedOptions = (ConnectorOptions)context.Options.Clone();
                    clonedOptions["Path"] = Path.ChangeExtension(newPath, _csvManager.Extension);
                    var newContext = new Context(clonedOptions);
                    await WriteAsync(newContext, data, cancellationToken);
                }
            }
        }
        else
        {
            if (!PathHelper.IsFile(path))
                throw new StreamException(Resources.ThePathIsNotFile);

            foreach (var row in transferData.Rows)
            {
                if (row.Items != null)
                {
                    dataTable.Rows.Add(row.Items);
                }
            }

            var data = _csvManager.ToCsv(dataTable, delimiter);

            var newPath = path;
            if (Path.GetExtension(path) != _csvManager.Extension)
            {
                _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                   $"So its extension will be automatically changed to {_csvManager.Extension}");

                newPath = Path.ChangeExtension(path, _csvManager.Extension);
            }

            var clonedOptions = (ConnectorOptions)context.Options.Clone();
            clonedOptions["Path"] = newPath;
            var newContext = new Context(clonedOptions);
            
            await WriteAsync(newContext, data, cancellationToken);
        }
    }

    public override Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var listOptions = context.Options.ToObject<ListOptions>();
        var compressOptions = context.Options.ToObject<CompressOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateCsvPerRow is false)
        {
            var content = _csvManager.ToCsv(filteredData, delimiter);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}{_csvManager.Extension}",
                ContentType = _csvManager.ContentType,
                Content = content.ToByteArray(),
            });

            return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
        }

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        foreach (DataRow row in filteredData.Rows)
        {
            try
            {
                var content = _csvManager.ToCsv(row, columnNames, delimiter);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{Guid.NewGuid().ToString()}{_csvManager.Extension}",
                    ContentType = _csvManager.ContentType,
                    Content = content.ToByteArray(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }

    #region internal methods
    private string GetDelimiter(string delimiter)
    {
        var defaultDelimiter = ",";
        if (_csvStreamSpecifications is null)
            return defaultDelimiter;

        var configDelimiter = string.IsNullOrEmpty(_csvStreamSpecifications.Delimiter)
            ? defaultDelimiter
            : _csvStreamSpecifications.Delimiter;

        return string.IsNullOrEmpty(delimiter) ? configDelimiter : delimiter;
    }

    private Task CreateEntityAsync(string entity, CreateOptions createOptions,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        if (File.Exists(path) && createOptions.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var headers = _deserializer.Deserialize<string[]>(createOptions.Headers);
        var data = string.Join(delimiter, headers);
        using (var writer = File.AppendText(path))
        {
            writer.WriteLine(data);
        }

        return Task.CompletedTask;
    }

    private string PrepareDataForWrite(WriteOptions writeOptions, DelimiterOptions delimiterOptions, object dataOptions)
    {
        var dataValue = dataOptions.GetObjectValue();

        if (dataValue is null)
            throw new StreamException(Resources.ForWritingDataMustHaveValue);

        if (dataValue is not string)
            throw new StreamException(Resources.DataMustBeInValidFormat);

        var sb = new StringBuilder();
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var headers = _deserializer.Deserialize<string[]>(writeOptions.Headers);
        var data = string.Join(delimiter, headers);
        var dataList = _deserializer.Deserialize<List<List<string>>>(dataValue.ToString());
        var append = writeOptions.OverWrite is false;

        if (!append && !string.IsNullOrEmpty(data))
            sb.AppendLine(data);

        foreach (var rowData in dataList)
        {
            sb.AppendLine(string.Join(delimiter, rowData));
        }

        return sb.ToString();
    }

    private Task WriteEntityAsync(string path, string content, bool append,
        CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        using (StreamWriter writer = new StreamWriter(path, append))
        {
            writer.WriteLine(content);
        }

        return Task.CompletedTask;
    }

    private async Task<ReadResult> ReadEntityAsync(string content, ReadOptions readOptions,
        ListOptions listOptions, DelimiterOptions delimiterOptions, 
        CancellationToken cancellationToken)
    {
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var entities = await FilteredEntitiesAsync(content, listOptions, delimiterOptions, cancellationToken)
                            .ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(Resources.NoItemsFoundWithTheGivenFilter),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = _csvManager.ToCsv(entities, delimiter).ToByteArray() }
        };
    }

    private async Task<DataTable> FilteredEntitiesAsync(string content, ListOptions listOptions,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var dataTable = await EntitiesAsync(content, listOptions, delimiterOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        return _dataFilter.Filter(dataTable, dataFilterOptions);
    }

    private Task<DataTable> EntitiesAsync(string content, ListOptions options,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var dataTable = GetDataTable(content, delimiter, options.IncludeMetadata, cancellationToken);

        return Task.FromResult(dataTable);
    }

    private async Task<string> ReadContent(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        if (context.ConnectorContext is not null)
        {
            var content = await context.ConnectorContext.Current.ReadAsync(new Context(context.Options), cancellationToken);
            return Encoding.UTF8.GetString(content.Content);
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task WriteContent(Context context, string content, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        if (context.ConnectorContext != null)
        {
            await context.ConnectorContext.Current.WriteAsync(new Context(context.Options), content, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    private DataTable GetDataTable(string content, string delimiter, 
        bool? includeMetadata, CancellationToken cancellationToken)
    {
        return _csvManager.Load(content, delimiter, includeMetadata);
    }

    private Task<TransferData> PrepareTransferring(Context context, CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var transferOptions = context.Options.ToObject<TransferOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), _csvManager.Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        var transferDataRows = new List<TransferDataRow>();
        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        var isSeparateCsvPerRow = transferOptions.SeparateCsvPerRow is true;
        var csvContentBase64 = string.Empty;

        if (!isSeparateCsvPerRow)
        {
            var csvContent = _csvManager.ToCsv(filteredData, delimiter);
            csvContentBase64 = csvContent.ToBase64String();
        }

        foreach (DataRow row in filteredData.Rows)
        {
            var itemArray = row.ItemArray;
            var content = isSeparateCsvPerRow ? _csvManager.ToCsv(row, columnNames, delimiter) : _csvManager.ToCsv(row, delimiter);
            transferDataRows.Add(new TransferDataRow
            {
                Key = $"{Guid.NewGuid().ToString()}{_csvManager.Extension}",
                ContentType = _csvManager.ContentType,
                Content = content.ToBase64String(),
                Items = itemArray
            });
        }

        var result = new TransferData
        {
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = TransferKind.Copy,
            ContentType = isSeparateCsvPerRow ? string.Empty : _csvManager.ContentType,
            Content = isSeparateCsvPerRow ? string.Empty : csvContentBase64,
            Columns = filteredData.Columns.Cast<DataColumn>().Select(x => x.ColumnName),
            Rows = transferDataRows
        };

        return Task.FromResult(result);
    }

    private DataFilterOptions GetDataFilterOptions(ListOptions options)
    {
        var fields = DeserializeToStringArray(options.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = options.Filter,
            SortExpression = options.Sort,
            CaseSensitive = options.CaseSensitive,
            Limit = options.Limit,
        };

        return dataFilterOptions;
    }

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }
    #endregion
}