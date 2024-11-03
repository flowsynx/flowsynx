using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.IO.Serialization;
using Newtonsoft.Json.Linq;
using System.Data;
using FlowSynx.Connectors.Stream.Exceptions;
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Abstractions.Extensions;
using Newtonsoft.Json;
using System.Text;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Compression;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Connectors.Stream.Json.Services;

public class JsonManager: IJsonManager
{
    private readonly ILogger _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private string ContentType => "application/json";
    private string Extension => ".json";

    public JsonManager(ILogger logger, IDataFilter dataFilter, IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _serializer = serializer;
    }

    public Task<object> About(Context context, CancellationToken cancellationToken)
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public Task CreateAsync(Context context, CancellationToken cancellationToken)
    {
        throw new StreamException(Resources.CreateOperrationNotSupported);
    }

    public async Task WriteAsync(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var content = PrepareDataForWrite(writeOptions, indentedOptions);
        if (context.ConnectorContext?.Current != null)
        {
            var clonedOptions = (ConnectorOptions)context.Options.Clone();
            clonedOptions["Data"] = content;
            var newContext = new Context(clonedOptions, context.ConnectorContext.Next);

            await context.ConnectorContext.Current.WriteAsync(newContext, cancellationToken).ConfigureAwait(false);
            return;
        }

        var append = writeOptions.OverWrite is false;
        await WriteLocallyAsync(pathOptions.Path, content, append).ConfigureAwait(false);
    }

    public async Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var content = await ReadContent(context, cancellationToken);
        return await ReadLocallyAsync(content, listOptions).ConfigureAwait(false);
    }

    public Task UpdateAsync(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistAsync(Context context, CancellationToken cancellationToken)
    {
        var filteredData = await FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);
        return filteredData.Rows.Count > 0;
    }
    
    public async Task<DataTable> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();
        var content = await ReadContent(context, cancellationToken);
        var dataTable = await JsonDataDataTableAsync(content, listOptions);
        var dataFilterOptions = GetFilterOptions(listOptions);
        return _dataFilter.Filter(dataTable, dataFilterOptions);
    }

    public async Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    CancellationToken cancellationToken)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);
        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public async Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        var transferOptions = context.Options.ToObject<TransferOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var dataTable = new DataTable();

        foreach (var column in transferData.Columns)
            dataTable.Columns.Add(column);

        if (transferOptions.SeparateJsonPerRow)
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

                    var data = ToJson(newRow, indentedOptions.Indented);
                    var newPath = transferData.Namespace == Namespace.Storage
                        ? row.Key
                        : PathHelper.Combine(path, row.Key);

                    if (Path.GetExtension(newPath) != Extension)
                    {
                        _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                           $"So its extension will be automatically changed to {Extension}");

                        newPath = Path.ChangeExtension(path, Extension);
                    }

                    var clonedOptions = (ConnectorOptions)context.Options.Clone();
                    clonedOptions["Path"] = Path.ChangeExtension(newPath, Extension);
                    clonedOptions["Data"] = data;
                    var newContext = new Context(clonedOptions);

                    await WriteAsync(newContext, cancellationToken);
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

            var data = ToJson(dataTable, indentedOptions.Indented);
            var newPath = path;
            if (Path.GetExtension(path) != Extension)
            {
                _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                   $"So its extension will be automatically changed to {Extension}");

                newPath = Path.ChangeExtension(path, Extension);
            }

            var clonedOptions = (ConnectorOptions)context.Options.Clone();
            clonedOptions["Path"] = newPath;
            clonedOptions["Data"] = data;
            var newContext = new Context(clonedOptions);

            await WriteAsync(newContext, cancellationToken);
        }
    }

    public async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var compressOptions = context.Options.ToObject<CompressOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var filteredData = await FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        if (compressOptions.SeparateJsonPerRow is false)
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

    private Task WriteLocallyAsync(string entity, string content, bool append)
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

        if (context.ConnectorContext?.Current is not null)
        {
            var content = await context.ConnectorContext.Current.ReadAsync(new Context(context.Options), cancellationToken);
            return Encoding.UTF8.GetString(content.Content);
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task<ReadResult> ReadLocallyAsync(string content, ListOptions listOptions)
    {
        var entities = await FilteredDataAsync(content, listOptions).ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = ToJson(entities, true).ToByteArray() }
        };
    }

    private async Task<DataTable> FilteredDataAsync(string content, ListOptions listOptions)
    {
        var dataTable = await JsonDataDataTableAsync(content, listOptions);
        var dataFilterOptions = GetFilterOptions(listOptions);
        return _dataFilter.Filter(dataTable, dataFilterOptions);
    }

    private Task<DataTable> JsonDataDataTableAsync(string json, ListOptions options)
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

    private DataFilterOptions GetFilterOptions(ListOptions options)
    {
        var fields = GetFields(options.Fields);
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

    private string[] GetFields(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private DataTable JArrayToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new DataTable();
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

    private DataTable JObjectToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new DataTable();
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

    private DataTable JPropertyToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new DataTable();
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

    private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, Context context,
        CancellationToken cancellationToken)
    {
        var transferOptions = context.Options.ToObject<TransferOptions>();
        var indentedOptions = context.Options.ToObject<IndentedOptions>();

        var filteredData = await FilteredEntitiesAsync(context, cancellationToken).ConfigureAwait(false);

        var isSeparateJsonPerRow = transferOptions.SeparateJsonPerRow;
        var jsonContentBase64 = string.Empty;

        var transferKind = GetTransferKind(transferOptions.TransferKind);
        if (!isSeparateJsonPerRow)
        {
            var jsonContent = ToJson(filteredData, indentedOptions.Indented);
            jsonContentBase64 = jsonContent.ToBase64String();
        }

        var result = new TransferData
        {
            Namespace = @namespace,
            ConnectorType = type,
            Kind = transferKind,
            ContentType = isSeparateJsonPerRow ? string.Empty : ContentType,
            Content = isSeparateJsonPerRow ? string.Empty : jsonContentBase64,
            Columns = GetColumnNames(filteredData),
            Rows = GenerateTransferDataRow(filteredData, indentedOptions.Indented)
        };

        return result;
    }

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
    
    private IEnumerable<string> GetColumnNames(DataTable dataTable)
    {
        return dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName);
    }

    private IEnumerable<TransferDataRow> GenerateTransferDataRow(DataTable dataTable, bool? indented = false)
    {
        var transferDataRows = new List<TransferDataRow>();
        foreach (DataRow row in dataTable.Rows)
        {
            var itemArray = row.ItemArray;
            var rowContent = ToJson(row, indented);
            transferDataRows.Add(new TransferDataRow
            {
                Key = $"{Guid.NewGuid()}{Extension}",
                ContentType = ContentType,
                Content = rowContent.ToBase64String(),
                Items = itemArray
            });
        }

        return transferDataRows;
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
    #endregion
}