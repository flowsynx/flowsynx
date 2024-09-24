using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Data.Extensions;
using System.Text;
using System.Data;

namespace FlowSynx.Plugin.Stream.Csv;

public class CsvStream : PluginBase
{
    private readonly ILogger _logger;
    private CsvStreamSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private readonly CsvHandler _csvHandler;

    public CsvStream(ILogger<CsvStream> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _dataFilter = dataFilter;
        _csvHandler = new CsvHandler(logger, serializer);
    }

    public override Guid Id => Guid.Parse("ce2fc15b-cd5e-4eb0-a5b4-22fa714e5cc9");
    public override string Name => "CSV";
    public override PluginNamespace Namespace => PluginNamespace.Stream;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(CsvStreamSpecifications);

    public override Task Initialize()
    {
        _csvStreamSpecifications = Specifications.ToObject<CsvStreamSpecifications>();
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override Task CreateAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && createOptions.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var delimiter = GetDelimiter(createOptions.Delimiter);
        var headers = _deserializer.Deserialize<string[]>(createOptions.Headers);
        var data = string.Join(delimiter, headers);
        using (var writer = File.AppendText(path))
        {
            writer.WriteLine(data);
        }

        return Task.CompletedTask;
    }

    public override Task WriteAsync(string entity, PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var dataValue = dataOptions.GetObjectValue();

        if (dataValue is null)
            throw new StreamException("Data must have value.");

        if (dataValue is not string)
            throw new StreamException("Data is not in valid format.");

        var delimiter = GetDelimiter(writeOptions.Delimiter);
        var dataList = _deserializer.Deserialize<List<List<string>>>(dataValue.ToString());
        using (var writer = File.AppendText(path))
        {
            foreach (var rowData in dataList)
            {
                writer.WriteLine(string.Join(delimiter, rowData));
            }
        }

        return Task.CompletedTask;
    }

    public override async Task<object> ReadAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var streamEntities = entities.ToList();
        if (!streamEntities.Any())
            throw new StreamException("string.Format(Resources.NoFilesFoundWithTheGivenFilter, path)");

        if (streamEntities.Count() > 1)
            throw new StreamException("The item you filter should be only single!");

        return streamEntities.First();
    }

    public override Task UpdateAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;
        var delimiter = GetDelimiter(listOptions.Delimiter);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = _csvHandler.Load(path, delimiter, listOptions.IncludeMetadata);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        _csvHandler.Delete(dataTable, filteredData);

        var result = filteredData.CreateListFromTable();
        var data = _csvHandler.ToCsv(dataTable, delimiter);
        await File.WriteAllTextAsync(path, data, cancellationToken);
    }

    public override async Task<bool> ExistAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);
        var streamEntities = entities.ToList();
        return streamEntities.Any();
    }

    public override Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var delimiter = GetDelimiter(listOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return Task.FromResult<IEnumerable<object>>(result);
    }

    public override Task<TransmissionData> PrepareTransmissionData(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var compressOptions = options.ToObject<CompressOptions>();

        var delimiter = GetDelimiter(listOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        var transmissionDataRows = new List<TransmissionDataRow>();
        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        const string contentType = "text/csv";
        var isSeparateCsvPerRow = compressOptions.SeparateCsvPerRow is true;
        var csvContentBase64 = string.Empty;

        if (!isSeparateCsvPerRow)
        {
            var csvContent = _csvHandler.Load(path, delimiter);
            csvContentBase64 = csvContent.ToBase64String();
        }

        foreach (DataRow row in filteredData.Rows)
        {
            var itemArray = row.ItemArray;
            var content = isSeparateCsvPerRow ? _csvHandler.ToCsv(row, columnNames, delimiter) : _csvHandler.ToCsv(row, delimiter);
            transmissionDataRows.Add(new TransmissionDataRow
            {
                Key = $"{Guid.NewGuid().ToString()}.csv",
                ContentType = contentType,
                Content = content.ToBase64String(),
                Items = itemArray
            });
        }
        
        var result = new TransmissionData
        {
            PluginNamespace = Namespace,
            PluginType = Type,
            ContentType = isSeparateCsvPerRow ? string.Empty : contentType,
            Content = isSeparateCsvPerRow ? string.Empty : csvContentBase64,
            Columns = filteredData.Columns.Cast<DataColumn>().Select(x => x.ColumnName),
            Rows = transmissionDataRows
        };

        return Task.FromResult(result);
    }

    public override Task TransmitDataAsync(string entity, PluginOptions? options,
        TransmissionData transmissionData, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var transmitOptions = options.ToObject<TransmitOptions>();
        var delimiter = GetDelimiter(transmitOptions.Delimiter);

        var dataTable = new DataTable();
        foreach (var column in transmissionData.Columns)
        {
            dataTable.Columns.Add(column);
        }

        foreach (var row in transmissionData.Rows)
        {
            if (row.Items != null)
                dataTable.Rows.Add(row.Items);
        }

        File.WriteAllText(path, _csvHandler.ToCsv(dataTable, delimiter));

        return Task.CompletedTask;
    }

    public override Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var compressOptions = options.ToObject<CompressOptions>();

        var delimiter = GetDelimiter(listOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateCsvPerRow is false)
        {
            var content = _csvHandler.ToCsv(filteredData, delimiter);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}.csv",
                ContentType = "text/csv",
                Stream = StringToStream(content),
            });

            return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
        }

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        foreach (DataRow row in filteredData.Rows)
        {
            try
            {
                var content = _csvHandler.ToCsv(row, columnNames, delimiter);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{Guid.NewGuid().ToString()}.csv",
                    ContentType = "text/csv",
                    Stream = StringToStream(content),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                continue;
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

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private System.IO.Stream StringToStream(string input)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(input);
        return new MemoryStream(byteArray);
    }

    private DataTable GetDataTable(string entity, string delimiter, 
        bool? includeMetadata, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        return _csvHandler.Load(path, delimiter, includeMetadata);
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
    #endregion
}