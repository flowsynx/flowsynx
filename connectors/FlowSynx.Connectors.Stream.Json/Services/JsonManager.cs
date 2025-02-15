﻿using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.IO.Serialization;
using Newtonsoft.Json.Linq;
using System.Data;
using FlowSynx.Connectors.Stream.Exceptions;
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Abstractions.Extensions;
using Newtonsoft.Json;
using System.Text;
using FlowSynx.IO.Compression;
using Microsoft.Extensions.Logging;
using FlowSynx.Data;
using FlowSynx.Data.Queries;

namespace FlowSynx.Connectors.Stream.Json.Services;

public class JsonManager : IJsonManager
{
    private readonly ILogger _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private string ContentType => "application/json";
    private string Extension => ".json";

    public JsonManager(ILogger logger, IDataService dataService, IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _dataService = dataService;
        _deserializer = deserializer;
        _serializer = serializer;
    }

    public Task Create(Context context, CancellationToken cancellationToken)
    {
        throw new StreamException(Resources.CreateOperrationNotSupported);
    }

    public async Task Write(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var content = PrepareDataForWrite(writeOptions, indentedOptions);
        //if (context.ConnectorContext?.Current != null)
        //{
        //    var clonedOptions = (ConnectorOptions)context.Options.Clone();
        //    clonedOptions["Data"] = content;
        //    var newContext = new Context(clonedOptions, context.ConnectorContext.Next);

        //    await context.ConnectorContext.Current.Write(newContext, cancellationToken).ConfigureAwait(false);
        //    return;
        //}

        var append = writeOptions.OverWrite is false;
        await WriteLocally(pathOptions.Path, content, append).ConfigureAwait(false);
    }

    public async Task<InterchangeData> Read(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var content = await ReadContent(context, cancellationToken);
        return await ReadLocally(content, listOptions).ConfigureAwait(false);
    }

    public Task Update(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Exist(Context context, CancellationToken cancellationToken)
    {
        var filteredData = await FilteredEntities(context, cancellationToken).ConfigureAwait(false);
        return filteredData.Rows.Count > 0;
    }

    public async Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var content = await ReadContent(context, cancellationToken);
        var dataTable = await JsonDataDataTable(content, listOptions);
        var dataFilterOptions = GetFilterOptions(listOptions);
        return (InterchangeData)_dataService.Select(dataTable, dataFilterOptions);
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
    //    var path = PathHelper.ToUnixPath(pathOptions.Path);

    //    var transferOptions = context.Options.ToObject<TransferOptions>();
    //    var indentedOptions = context.Options.ToObject<IndentedOptions>();

    //    var dataTable = new DataTable();

    //    foreach (var column in transferData.Columns)
    //    {
    //        if (column.DataType is null)
    //            dataTable.Columns.Add(column.Name);
    //        else
    //            dataTable.Columns.Add(column.Name, column.DataType);
    //    }

    //    if (transferOptions.SeparateDataPerRow)
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

    //                var data = ToJson(newRow, indentedOptions.Indented);
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

    //        var data = ToJson(dataTable, indentedOptions.Indented);
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

        var compressOptions = context.Options.ToObject<CompressOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var filteredData = await FilteredEntities(context, cancellationToken).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        if (compressOptions.SeparateDataPerRow is false)
            return await CompressDataTable(filteredData, indentedOptions.Indented);

        return await CompressDataRows(filteredData.Rows, indentedOptions.Indented);
    }

    #region internal methods
    private dynamic PrepareDataForWrite(WriteOptions writeOptions, IndentedOptions indentedOptions)
    {
        var dataValue = writeOptions.Data.GetObjectValue();

        if (dataValue is null)
            throw new StreamException(Resources.ForWritingDataMustHaveValue);

        if (dataValue is not string json)
            throw new StreamException(Resources.DataMustBeInValidFormat);

        if (!IsValidJson(json))
            throw new StreamException(Resources.DataMustBeJsonValidFormat);

        var deserializeData = _deserializer.Deserialize<dynamic>(json);
        var serializeData = _serializer.Serialize(deserializeData, new JsonSerializationConfiguration
        {
            Indented = indentedOptions.Indented ?? false,
        });

        return serializeData;
    }

    private bool IsValidJson(string? jsonString)
    {
        try
        {
            if (jsonString == null)
                return true;

            JToken.Parse(jsonString);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    private Task WriteLocally(string entity, string content, bool append)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var parentPath = PathHelper.GetParent(path);
        Directory.CreateDirectory(parentPath);

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

        //if (context.ConnectorContext?.Current is not null)
        //{
        //    var content = await context.ConnectorContext.Current.Read(new Context(context.Options), cancellationToken);
        //    return Encoding.UTF8.GetString((byte[])content.Rows[0]["Content"]);
        //}

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task<InterchangeData> ReadLocally(string content, ListOptions listOptions)
    {
        var entities = await FilteredData(content, listOptions).ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => ReadData(ToJson(entities, true))
        };
    }

    private InterchangeData ReadData(string content)
    {
        var result = new InterchangeData();
        result.Columns.Add("Content", typeof(byte[]));
        result.Rows.Add(content.ToByteArray());
        return result;
    }

    private async Task<DataTable> FilteredData(string content, ListOptions listOptions)
    {
        var dataTable = await JsonDataDataTable(content, listOptions);
        var dataFilterOptions = GetFilterOptions(listOptions);
        return _dataService.Select(dataTable, dataFilterOptions);
    }

    private Task<InterchangeData> JsonDataDataTable(string json, ListOptions options)
    {
        var jToken = JToken.Parse(json);
        var dataTable = jToken switch
        {
            JArray => JArrayToDataTable(jToken, options.IncludeMetadata),
            JObject => JObjectToDataTable(jToken, options.IncludeMetadata),
            _ => JPropertyToDataTable(jToken, options.IncludeMetadata)
        };

        return Task.FromResult(dataTable);
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

    private InterchangeData JArrayToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new InterchangeData();
        foreach (var row in token)
        {
            var dict = Flatten(row);
            if (includeMetaData is true)
            {
                var metadataObject = GetMetaData(dict);
                if (!dict.ContainsKey("Metadata"))
                    dict.Add("Metadata", metadataObject);
            }

            var dataRow = result.NewRow();
            foreach (var item in dict)
            {
                if (result.Columns[item.Key] == null)
                {
                    var type = item.Value is null ? typeof(string) : item.Value.GetType();
                    result.Columns.Add(item.Key, type);
                }

                dataRow[item.Key] = item.Value;

            }
            result.Rows.Add(dataRow);
        }

        return result;
    }

    private InterchangeData JObjectToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new InterchangeData();
        var dict = Flatten(token);

        if (includeMetaData is true)
        {
            var metadataObject = GetMetaData(dict);
            if (!dict.ContainsKey("Metadata"))
                dict.Add("Metadata", metadataObject);
        }

        var dataRow = result.NewRow();
        foreach (var item in dict)
        {
            if (result.Columns[item.Key] == null)
            {
                var type = item.Value is null ? typeof(string) : item.Value.GetType();
                result.Columns.Add(item.Key, type);
            }

            dataRow[item.Key] = item.Value;
        }

        result.Rows.Add(dataRow);
        return result;
    }

    private InterchangeData JPropertyToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new InterchangeData();
        var dict = Flatten(token);

        if (includeMetaData is true)
        {
            var metadataObject = GetMetaData(dict);
            if (!dict.ContainsKey("Metadata"))
                dict.Add("Metadata", metadataObject);
        }

        var dataRow = result.NewRow();
        foreach (var item in dict)
        {
            if (result.Columns[item.Key] == null)
            {
                var type = item.Value is null ? typeof(string) : item.Value.GetType();
                result.Columns.Add(item.Key, type);
            }

            dataRow[item.Key] = item.Value;
        }

        result.Rows.Add(dataRow);
        return result;
    }

    private object GetMetaData(object content)
    {
        var contentHash = Security.HashHelper.Md5.GetHash(content);
        return new
        {
            ContentHash = contentHash
        };
    }

    private TransferKind GetTransferKind(string? kind)
    {
        if (string.IsNullOrEmpty(kind))
            return TransferKind.Copy;

        if (string.Equals(kind, "copy", StringComparison.OrdinalIgnoreCase))
            return TransferKind.Copy;

        if (string.Equals(kind, "move", StringComparison.OrdinalIgnoreCase))
            return TransferKind.Move;

        throw new StreamException("Transfer Kind is not supported. Its value should be Copy or Move.");
    }

    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, Context context,
    //    CancellationToken cancellationToken)
    //{
    //    var transferOptions = context.Options.ToObject<TransferOptions>();
    //    var indentedOptions = context.Options.ToObject<IndentedOptions>();

    //    var filteredData = await FilteredEntities(context, cancellationToken).ConfigureAwait(false);

    //    var isSeparateJsonPerRow = transferOptions.SeparateDataPerRow;
    //    var jsonContentBase64 = string.Empty;
        
    //    if (!isSeparateJsonPerRow)
    //    {
    //        var jsonContent = ToJson(filteredData, indentedOptions.Indented);
    //        jsonContentBase64 = jsonContent.ToBase64String();
    //    }

    //    var result = new TransferData
    //    {
    //        Namespace = @namespace,
    //        ConnectorType = type,
    //        ContentType = isSeparateJsonPerRow ? string.Empty : ContentType,
    //        Content = isSeparateJsonPerRow ? string.Empty : jsonContentBase64,
    //        Columns = GetTransferDataColumn(filteredData),
    //        Rows = GenerateTransferDataRow(filteredData, indentedOptions.Indented)
    //    };

    //    return result;
    //}

    private Task<IEnumerable<CompressEntry>> CompressDataTable(DataTable dataTable, bool? indented)
    {
        var compressEntries = new List<CompressEntry>();
        var rowContent = ToJson(dataTable, indented);
        compressEntries.Add(new CompressEntry
        {
            Name = $"{Guid.NewGuid().ToString()}{Extension}",
            ContentType = ContentType,
            Content = rowContent.ToByteArray(),
        });

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }

    private Task<IEnumerable<CompressEntry>> CompressDataRows(DataRowCollection dataRows, bool? indented)
    {
        var compressEntries = new List<CompressEntry>();
        foreach (DataRow row in dataRows)
        {
            try
            {
                var rowContent = ToJson(row, indented);
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

    private string ToJson(DataTable dataTable, bool? indented)
    {
        var jsonString = string.Empty;

        if (dataTable is { Rows.Count: > 0 })
        {
            var config = new JsonSerializationConfiguration { Indented = indented ?? true };
            jsonString = _serializer.Serialize(dataTable, config);
        }

        return jsonString;
    }

    private string ToJson(DataRow dataRow, bool? indented)
    {
        var dict = new Dictionary<string, object>();
        foreach (DataColumn col in dataRow.Table.Columns)
        {
            dict.Add(col.ColumnName, dataRow[col]);
        }

        var config = new JsonSerializationConfiguration { Indented = indented ?? true };
        return _serializer.Serialize(dict, config);
    }

    private IDictionary<string, object?> Flatten(JToken token, string prefix = "")
    {
        var result = new Dictionary<string, object?>();
        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                var childPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}_{property.Name}";
                foreach (var child in Flatten(property.Value, childPrefix))
                {
                    result.Add(child.Key, child.Value);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            var index = 1;
            foreach (var item in token.Children())
            {
                var childPrefix = $"{prefix}{index}";
                foreach (var child in Flatten(item, childPrefix))
                {
                    result.Add(child.Key, child.Value);
                }
                index++;
            }
        }
        else
        {
            result.Add(prefix, ((JValue)token).Value);
        }

        return result;
    }
    
    //private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    //{
    //    return dataTable.Columns.Cast<DataColumn>()
    //        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    //}

    //private IEnumerable<TransferDataRow> GenerateTransferDataRow(DataTable dataTable, bool? indented = false)
    //{
    //    var transferDataRows = new List<TransferDataRow>();
    //    foreach (DataRow row in dataTable.Rows)
    //    {
    //        var itemArray = row.ItemArray;
    //        var rowContent = ToJson(row, indented);
    //        transferDataRows.Add(new TransferDataRow
    //        {
    //            Key = $"{Guid.NewGuid()}{Extension}",
    //            ContentType = ContentType,
    //            Content = rowContent.ToBase64String(),
    //            Items = itemArray
    //        });
    //    }

    //    return transferDataRows;
    //}

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
    #endregion
}