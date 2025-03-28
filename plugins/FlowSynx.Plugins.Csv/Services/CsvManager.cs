﻿using System.Data;
using System.Text;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Data.Queries;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Stream.Csv.Services;

internal class CsvManager: ICsvManager
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly CsvSpecifications _specifications;

    private string ContentType => "text/csv";
    private string Extension => ".csv";

    public CsvManager(ILogger logger, IDataService dataService, IDeserializer deserializer, 
        CsvSpecifications specifications)
    {
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _specifications = specifications;
    }

    public Task Create(Context context, CancellationToken cancellationToken)
    {
        throw new StreamException(Resources.CreateOperrationNotSupported);
    }

    public async Task Write(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        var content = PrepareDataForWrite(writeOptions, delimiterOptions);
        var append = writeOptions.OverWrite is false;
        await WriteLocally(pathOptions.Path, content, append, cancellationToken).ConfigureAwait(false);
    }

    //public async Task<InterchangeData> Read(Context context, CancellationToken cancellationToken)
    //{
    //    var readOptions = context.Options.ToObject<ReadOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();
    //    var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

    //    var content = await ReadContent(context, cancellationToken);

    //    return await ReadLocally(content, readOptions, listOptions, delimiterOptions, cancellationToken).ConfigureAwait(false);
    //}

    public Task Update(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Context context, CancellationToken cancellationToken)
    {
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        var content = await ReadContent(context, cancellationToken);
        var dataFilterOptions = GetFilterOptions(listOptions);

        var dataTable = GetData(content, delimiter, listOptions.IncludeMetadata);
        var filteredData = GetFilteredData(content, delimiter, listOptions);
        Delete(dataTable, filteredData);

        var data = ToCsv(dataTable, delimiter);

        await WriteContent(context, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Exist(Context context, CancellationToken cancellationToken)
    {
        var filteredData = await FilteredEntities(context, cancellationToken).ConfigureAwait(false);
        return filteredData.Rows.Count > 0;
    }

    public async Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken)
    {
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var content = await ReadContent(context, cancellationToken);
        return GetFilteredData(content, delimiter, listOptions);
    }

    public Task Transfer(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken)
    //{
    //    if (destinationContext.ConnectorContext?.Current is null)
    //        throw new StreamException(Resources.CalleeConnectorNotSupported);

    //    var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);
    //    await destinationContext.ConnectorContext.Current.ProcessTransfer(destinationContext, transferData, transferKind, cancellationToken);
    //}

    //public async Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var transferOptions = context.Options.ToObject<TransferOptions>();
    //    var delimiterOptions = context.Options.ToObject<DelimiterOptions>();
    //    var path = PathHelper.ToUnixPath(pathOptions.Path);

    //    var delimiter = GetDelimiter(delimiterOptions.Delimiter);

    //    var dataTable = new DataTable();

    //    foreach (var column in transferData.Columns)
    //    {
    //        if (column.DataType is null)
    //            dataTable.Columns.Add(column.Name);
    //        else
    //            dataTable.Columns.Add(column.Name, column.DataType);
    //    }

    //    if (transferOptions.SeparateDataPerRow is true)
    //    {
    //        if (!PathHelper.IsDirectory(path))
    //            throw new StreamException(Resources.ThePathIsNotDirectory);

    //        foreach (var row in transferData.Rows)
    //        {
    //            if (row.Items != null)
    //            {
    //                var newRow = dataTable.NewRow();
    //                newRow.ItemArray = row.Items;
    //                dataTable.Rows.Add(newRow);

    //                var data = ToCsv(newRow, transferData.Columns.Select(x=>x.Name).ToArray(), delimiter);
    //                var newPath = transferData.Namespace == Namespace.Storage
    //                    ? row.Key
    //                    : PathHelper.Combine(path, row.Key);

    //                if (Path.GetExtension(newPath) != Extension)
    //                {
    //                    _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
    //                                       $"So its extension will be automatically changed to {Extension}");

    //                    newPath = Path.ChangeExtension(path, Extension);
    //                }

    //                var clonedOptions = (ConnectorOptions)context.Options.Clone();
    //                clonedOptions["Path"] = Path.ChangeExtension(newPath, Extension);
    //                clonedOptions["Data"] = data;
    //                var newContext = new Context(clonedOptions);

    //                await Write(newContext, cancellationToken);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        if (!PathHelper.IsFile(path))
    //            throw new StreamException(Resources.ThePathIsNotFile);

    //        foreach (var row in transferData.Rows)
    //        {
    //            if (row.Items != null)
    //            {
    //                dataTable.Rows.Add(row.Items);
    //            }
    //        }

    //        var data = ToCsv(dataTable, delimiter);

    //        var newPath = path;
    //        if (Path.GetExtension(path) != Extension)
    //        {
    //            _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
    //                               $"So its extension will be automatically changed to {Extension}");

    //            newPath = Path.ChangeExtension(path, Extension);
    //        }

    //        var clonedOptions = (ConnectorOptions)context.Options.Clone();
    //        clonedOptions["Path"] = newPath;
    //        clonedOptions["Data"] = data;
    //        var newContext = new Context(clonedOptions);

    //        await Write(newContext, cancellationToken);
    //    }
    //}

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!IsCsvFile(path))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        var filteredData = await FilteredEntities(context, cancellationToken);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressOptions = context.Options.ToObject<CompressOptions>();
        var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

        if (compressOptions.SeparateDataPerRow is false)
            return await CompressDataTable(filteredData, delimiterOptions);

        return await CompressDataRows(filteredData.Rows, delimiterOptions);
    }

    #region internal methods
    private string PrepareDataForWrite(WriteOptions writeOptions, DelimiterOptions delimiterOptions)
    {
        var dataValue = writeOptions.Data.GetObjectValue();

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

    private Task WriteLocally(string path, string content, bool append,
    CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        using (StreamWriter writer = new StreamWriter(path, append))
        {
            writer.WriteLine(content);
        }

        return Task.CompletedTask;
    }

    private async Task<string> ReadContent(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        //if (context.ConnectorContext is not null)
        //{
        //    var content = await context.ConnectorContext.Current.Read(new Context(context.Options), cancellationToken);
        //    return Encoding.UTF8.GetString((byte[])content.Rows[0]["Content"]);
        //}

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private Task<InterchangeData> ReadLocally(string content, ReadOptions readOptions,
    ListOptions listOptions, DelimiterOptions delimiterOptions,
    CancellationToken cancellationToken)
    {
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var entities = GetFilteredData(content, delimiter, listOptions);

        var result = entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(Resources.NoItemsFoundWithTheGivenFilter),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => ReadData(ToCsv(entities, delimiter))
        };

        return Task.FromResult(result);
    }

    private InterchangeData ReadData(string content)
    {
        var result = new InterchangeData();
        result.Columns.Add("Content", typeof(byte[]));
        result.Rows.Add(content.ToByteArray());
        return result;
    }

    private async Task WriteContent(Context context, string content, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        //if (context.ConnectorContext != null)
        //{
        //    var clonedOptions = (ConnectorOptions)context.Options.Clone();
        //    clonedOptions["Path"] = path;
        //    clonedOptions["Data"] = content;
        //    var newContext = new Context(clonedOptions);

        //    await context.ConnectorContext.Current.Write(newContext, cancellationToken).ConfigureAwait(false);
        //    return;
        //}

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            throw new StreamException(Resources.ThePathIsNotCsvFile);

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    private InterchangeData GetFilteredData(string content, string delimiter, ListOptions listOptions)
    {
        var dataTable = GetData(content, delimiter, listOptions.IncludeMetadata);
        var dataFilterOptions = GetFilterOptions(listOptions);
        return (InterchangeData)_dataService.Select(dataTable, dataFilterOptions);
    }

    private InterchangeData GetData(string content, string delimiter, bool? includeMetadata)
    {
        return Load(content, delimiter, includeMetadata ?? false);
    }

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, Context context,
    //    CancellationToken cancellationToken)
    //{
    //    var pathOptions = context.Options.ToObject<PathOptions>();
    //    var listOptions = context.Options.ToObject<ListOptions>();
    //    var transferOptions = context.Options.ToObject<TransferOptions>();
    //    var delimiterOptions = context.Options.ToObject<DelimiterOptions>();

    //    var path = PathHelper.ToUnixPath(pathOptions.Path);
    //    if (string.IsNullOrEmpty(path))
    //        throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

    //    if (!PathHelper.IsFile(path))
    //        throw new StreamException(Resources.ThePathIsNotFile);

    //    if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
    //        throw new StreamException(Resources.ThePathIsNotCsvFile);

    //    var delimiter = GetDelimiter(delimiterOptions.Delimiter);

    //    var filteredData = await FilteredEntities(context, cancellationToken);

    //    var transferDataRows = new List<TransferDataRow>();
    //    var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //    var isSeparateCsvPerRow = transferOptions.SeparateDataPerRow is true;
    //    var csvContentBase64 = string.Empty;

    //    if (!isSeparateCsvPerRow)
    //    {
    //        var csvContent = ToCsv(filteredData, delimiter);
    //        csvContentBase64 = csvContent.ToBase64String();
    //    }

    //    foreach (DataRow row in filteredData.Rows)
    //    {
    //        var itemArray = row.ItemArray;
    //        var content = isSeparateCsvPerRow ? ToCsv(row, columnNames, delimiter) : ToCsv(row, delimiter);
    //        transferDataRows.Add(new TransferDataRow
    //        {
    //            Key = $"{Guid.NewGuid().ToString()}{Extension}",
    //            ContentType = ContentType,
    //            Content = content.ToBase64String(),
    //            Items = itemArray
    //        });
    //    }

    //    return new TransferData
    //    {
    //        Namespace = @namespace,
    //        ConnectorType = type,
    //        ContentType = isSeparateCsvPerRow ? string.Empty : ContentType,
    //        Content = isSeparateCsvPerRow ? string.Empty : csvContentBase64,
    //        Columns = GetTransferDataColumn(filteredData),
    //        Rows = transferDataRows
    //    };
    //}

    private Task<IEnumerable<CompressEntry>> CompressDataTable(DataTable dataTable, DelimiterOptions delimiterOptions)
    {
        var compressEntries = new List<CompressEntry>();
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);
        var rowContent = ToCsv(dataTable, delimiter);

        compressEntries.Add(new CompressEntry
        {
            Name = $"{Guid.NewGuid().ToString()}{Extension}",
            ContentType = ContentType,
            Content = rowContent.ToByteArray(),
        });

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }

    private Task<IEnumerable<CompressEntry>> CompressDataRows(DataRowCollection dataRows, DelimiterOptions delimiterOptions)
    {
        var compressEntries = new List<CompressEntry>();
        var delimiter = GetDelimiter(delimiterOptions.Delimiter);

        foreach (DataRow row in dataRows)
        {
            try
            {
                var rowContent = ToCsv(row, delimiter);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{Guid.NewGuid().ToString()}{Extension}",
                    ContentType = ContentType,
                    Content = rowContent.ToByteArray(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }

    private bool IsCsvFile(string path)
    {
        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private SelectDataOption GetFilterOptions(ListOptions options)
    {
        var dataFilterOptions = new SelectDataOption()
        {
            Fields = GetFields(options.Fields),
            Filter = GetFilterList(options.Filters),
            Sort = GetSortList(options.Sort),
            Paging = GetPaging(options.Paging),
            CaseSensitive = options.CaseSensitive
        };

        return dataFilterOptions;
    }

    private FieldsList GetFields(string? json)
    {
        var result = new FieldsList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FieldsList>(json);
        }

        return result;
    }

    private FilterList GetFilterList(string? json)
    {
        var result = new FilterList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FilterList>(json);
        }

        return result;
    }

    private SortList GetSortList(string? json)
    {
        var result = new SortList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<SortList>(json);
        }

        return result;
    }

    private Paging GetPaging(string? json)
    {
        var result = new Paging();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<Paging>(json);
        }

        return result;
    }

    private string GetDelimiter(string delimiter)
    {
        const string defaultDelimiter = ",";
        var configDelimiter = string.IsNullOrEmpty(_specifications.Delimiter)
            ? defaultDelimiter
            : _specifications.Delimiter;

        return string.IsNullOrEmpty(delimiter) ? configDelimiter : delimiter;
    }

    private byte[] Load(string fullPath)
    {
        return File.ReadAllBytes(fullPath);
    }

    private InterchangeData Load(string content, string delimiter, bool? includeMetaData)
    {
        var dataTable = new InterchangeData();
        var lines = content.Split(System.Environment.NewLine);

        if (lines.Length == 0)
            return dataTable;

        // Add columns
        var headers = lines[0].Split(delimiter, StringSplitOptions.None);

        if (includeMetaData is true)
            headers = headers.Append("Metadata").ToArray();

        foreach (var header in headers)
            dataTable.Columns.Add(header);

        var columnsCount = headers.Length;

        // Add rows
        for (var i = 1; i < lines.Length - 1; i++)
        {
            object[] fields = lines[i].Split(delimiter, StringSplitOptions.None);
            if (includeMetaData is true)
            {
                var metadataObject = GetMetaData(fields);
                fields = fields.Append(metadataObject).ToArray();
            }

            var currentLength = fields.Length;
            if (currentLength != columnsCount)
                continue;

            var dataRow = dataTable.NewRow();
            for (var j = 0; j < headers.Length; j++)
            {
                dataRow[j] = fields[j];
            }

            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }

    private string ToCsv(DataTable dataTable, string delimiter)
    {
        var stringBuilder = new StringBuilder();

        var columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        stringBuilder.AppendLine(string.Join(delimiter, columnNames));

        foreach (DataRow row in dataTable.Rows)
        {
            var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
            stringBuilder.AppendLine(string.Join(delimiter, fields));
        }

        return stringBuilder.ToString();
    }

    private string ToCsv(DataRow row, string[] headers, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(string.Join(delimiter, headers));

        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));

        return stringBuilder.ToString();
    }

    private string ToCsv(DataRow row, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));
        return stringBuilder.ToString();
    }

    private void Delete(DataTable allRows, DataTable rowsToDelete)
    {
        foreach (DataRow rowToDelete in rowsToDelete.Rows)
        {
            foreach (DataRow row in allRows.Rows)
            {
                var rowToDeleteArray = rowToDelete.ItemArray;
                var rowArray = row.ItemArray;
                var equalRows = true;
                for (var i = 0; i < rowArray.Length; i++)
                {
                    if (!rowArray[i]!.Equals(rowToDeleteArray[i]))
                    {
                        equalRows = false;
                    }
                }

                if (!equalRows)
                    continue;

                allRows.Rows.Remove(row);
                break;
            }
        }
    }

    private object GetMetaData(object content)
    {
        var contentHash = Security.HashHelper.Md5.GetHash(content);
        return contentHash;
    }

    //private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    //{
    //    return dataTable.Columns.Cast<DataColumn>()
    //        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    //}
    #endregion
}