﻿using FlowSynx.Data.Filter;
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
    private const string ContentType = "text/csv";
    private const string Extension = ".csv";

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

    public override Task<object> About(PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var createOptions = options.ToObject<CreateOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();
        await CreateEntityAsync(entity, createOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var writeOptions = options.ToObject<WriteOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();
        await WriteEntityAsync(entity, writeOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var readOptions = options.ToObject<ReadOptions>();
        var listOptions = options.ToObject<ListOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();
        return await ReadEntityAsync(entity, readOptions, listOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var delimiterOptions = options.ToObject<DelimiterOptions>();
        var listOptions = options.ToObject<ListOptions>();
        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = _csvHandler.Load(path, delimiter, listOptions.IncludeMetadata);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        _csvHandler.Delete(dataTable, filteredData);

        var result = filteredData.CreateListFromTable();
        var data = _csvHandler.ToCsv(dataTable, delimiter);
        await File.WriteAllTextAsync(path, data, cancellationToken);
    }

    public override async Task<bool> ExistAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var listOptions = options.ToObject<ListOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();
        var filteredData = await FilteredEntitiesAsync(entity, listOptions, delimiterOptions, cancellationToken)
                                .ConfigureAwait(false);
        return filteredData.Rows.Count > 0;
    }

    public override Task<IEnumerable<object>> ListAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var listOptions = options.ToObject<ListOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return Task.FromResult<IEnumerable<object>>(result);
    }

    public override Task<TransferData> PrepareTransferring(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var listOptions = options.ToObject<ListOptions>();
        var transferOptions = options.ToObject<TransferOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();

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
            var csvContent = _csvHandler.ToCsv(filteredData, delimiter);
            csvContentBase64 = csvContent.ToBase64String();
        }

        foreach (DataRow row in filteredData.Rows)
        {
            var itemArray = row.ItemArray;
            var content = isSeparateCsvPerRow ? _csvHandler.ToCsv(row, columnNames, delimiter) : _csvHandler.ToCsv(row, delimiter);
            transferDataRows.Add(new TransferDataRow
            {
                Key = $"{Guid.NewGuid().ToString()}{Extension}",
                ContentType = ContentType,
                Content = content.ToBase64String(),
                Items = itemArray
            });
        }
        
        var result = new TransferData
        {
            PluginNamespace = Namespace,
            PluginType = Type,
            Kind = TransferKind.Copy,
            ContentType = isSeparateCsvPerRow ? string.Empty : ContentType,
            Content = isSeparateCsvPerRow ? string.Empty : csvContentBase64,
            Columns = filteredData.Columns.Cast<DataColumn>().Select(x => x.ColumnName),
            Rows = transferDataRows
        };

        return Task.FromResult(result);
    }

    public override Task TransferAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, TransferData transferData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var transferOptions = options.ToObject<TransferOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataTable = new DataTable();
        foreach (var column in transferData.Columns)
        {
            dataTable.Columns.Add(column);
        }

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
                    File.WriteAllText(PathHelper.Combine(path, row.Key), 
                        _csvHandler.ToCsv(newRow, transferData.Columns.ToArray(), delimiter));
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

            File.WriteAllText(path, _csvHandler.ToCsv(dataTable, delimiter));
        }

        return Task.CompletedTask;
    }

    public override Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var listOptions = options.ToObject<ListOptions>();
        var compressOptions = options.ToObject<CompressOptions>();
        var delimiterOptions = options.ToObject<DelimiterOptions>();

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = GetDataTable(path, delimiter, listOptions.IncludeMetadata, cancellationToken);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateCsvPerRow is false)
        {
            var content = _csvHandler.ToCsv(filteredData, delimiter);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}{Extension}",
                ContentType = ContentType,
                Content = StringToByteArray(content),
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
                    Name = $"{Guid.NewGuid().ToString()}{Extension}",
                    ContentType = ContentType,
                    Content = StringToByteArray(content),
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

    private Task CreateEntityAsync(string entity, CreateOptions createOptions,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
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

    private Task WriteEntityAsync(string entity, WriteOptions writeOptions,
        DelimiterOptions delimiterOptions, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var dataValue = dataOptions.GetObjectValue();

        if (dataValue is null)
            throw new StreamException(Resources.ForWritingDataMustHaveValue);

        if (dataValue is not string)
            throw new StreamException(Resources.DataMustBeInValidFormat);

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
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

    private async Task<ReadResult> ReadEntityAsync(string entity, ReadOptions readOptions,
        ListOptions listOptions, DelimiterOptions delimiterOptions, 
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await FilteredEntitiesAsync(path, listOptions, delimiterOptions, cancellationToken)
                            .ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = ObjectToByteArray(entities) }
        };
    }

    private async Task<DataTable> FilteredEntitiesAsync(string entity, ListOptions listOptions,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        var dataTable = await EntitiesAsync(path, listOptions, delimiterOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var result = _dataFilter.Filter(dataTable, dataFilterOptions);
        return result;
    }

    private Task<DataTable> EntitiesAsync(string entity, ListOptions options,
        DelimiterOptions delimiterOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var dataTable = GetDataTable(path, delimiter, options.IncludeMetadata, cancellationToken);

        return Task.FromResult(dataTable);
    }

    private DataTable GetDataTable(string entity, string delimiter, 
        bool? includeMetadata, CancellationToken cancellationToken = new CancellationToken())
    {
        return _csvHandler.Load(entity, delimiter, includeMetadata);
    }

    private byte[] ObjectToByteArray(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Convert object to byte array here
                writer.Write((int)obj);
            }
            return stream.ToArray();
        }
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

    private byte[] StringToByteArray(string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }
    #endregion
}