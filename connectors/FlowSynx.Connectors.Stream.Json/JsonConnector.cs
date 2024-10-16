using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions;
using Newtonsoft.Json.Linq;
using System.Data;
using FlowSynx.IO;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Data.Extensions;
using FlowSynx.Connectors.Stream.Exceptions;
using Newtonsoft.Json;
using System.Text;
using FlowSynx.Connectors.Stream.Json.Options;

namespace FlowSynx.Connectors.Stream.Json;

public class JsonConnector : Connector
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private readonly IDataFilter _dataFilter;
    private readonly JsonHandler _jsonHandler;
    public JsonConnector(ILogger<JsonConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _serializer = serializer;
        _dataFilter = dataFilter;
        _jsonHandler = new JsonHandler(serializer);
    }

    public override Guid Id => Guid.Parse("0914e754-b203-4f37-9ac2-c67d86400eb9");
    public override string Name => "Json";
    public override Namespace Namespace => Namespace.Stream;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(JsonStreamSpecifications);

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override Task CreateAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.CreateOperrationNotSupported);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options,
        object dataOptions, CancellationToken cancellationToken = default)
    {
        var writeOptions = options.ToObject<WriteOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var content = PrepareDataForWrite(writeOptions, indentedOptions, dataOptions);
        if (context.Connector != null)
        {
            await context.Connector.WriteAsync(new Context(context.Entity), options, content, cancellationToken).ConfigureAwait(false);
            return;
        }

        var append = writeOptions.Overwite is false;
        await WriteEntityAsync(context.Entity, content, append, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);

        return await ReadEntityAsync(content, listOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, cancellationToken).ConfigureAwait(false);

        return filteredData.Rows.Count > 0;
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, cancellationToken).ConfigureAwait(false);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    private async Task<TransferData> PrepareTransferring(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);

        var listOptions = options.ToObject<ListOptions>();
        var transferOptions = options.ToObject<TransferOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, cancellationToken).ConfigureAwait(false);

        var isSeparateJsonPerRow = transferOptions.SeparateJsonPerRow is true;
        var jsonContentBase64 = string.Empty;

        var transferKind = GetTransferKind(transferOptions.TransferKind);
        if (!isSeparateJsonPerRow)
        {
            var jsonContent = _jsonHandler.ToJson(filteredData, indentedOptions.Indented);
            jsonContentBase64 = jsonContent.ToBase64String();
        }

        var result = new TransferData
        {
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = transferKind,
            ContentType = isSeparateJsonPerRow ? string.Empty : _jsonHandler.ContentType,
            Content = isSeparateJsonPerRow ? string.Empty : jsonContentBase64,
            Columns = _jsonHandler.GetColumnNames(filteredData),
            Rows = _jsonHandler.GenerateTransferDataRow(filteredData, indentedOptions.Indented)
        };

        return result;
    }

    public override Task TransferAsync(Context destinationContext, Connector? sourceConnector,
        Context sourceContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        //var path = PathHelper.ToUnixPath(entity);
        //var transferOptions = options.ToObject<TransferOptions>();
        //var indentedOptions = options.ToObject<IndentedOptions>();

        //var dataTable = new DataTable();
        //foreach (var column in transferData.Columns)
        //{
        //    dataTable.Columns.Add(column);
        //}

        //if (transferOptions.SeparateJsonPerRow is true)
        //{
        //    if (!PathHelper.IsDirectory(path))
        //        throw new StreamException(Resources.ThePathIsNotDirectory);

        //    foreach (var row in transferData.Rows)
        //    {
        //        if (row.Items != null)
        //        {
        //            var newRow = dataTable.NewRow();
        //            newRow.ItemArray = row.Items;
        //            dataTable.Rows.Add(newRow);
        //            File.WriteAllText(PathHelper.Combine(path, row.Key), _jsonHandler.ToJson(newRow, indentedOptions.Indented));
        //        }
        //    }
        //}
        //else
        //{
        //    if (!PathHelper.IsFile(path))
        //        throw new StreamException(Resources.ThePathIsNotFile);

        //    foreach (var row in transferData.Rows)
        //    {
        //        if (row.Items != null)
        //        {
        //            dataTable.Rows.Add(row.Items);
        //        }
        //    }

        //    File.WriteAllText(path, _jsonHandler.ToJson(dataTable, indentedOptions.Indented));
        //}

        return Task.CompletedTask;
    }

    public override async Task ProcessTransferAsync(Context sourceContext, TransferData transferData,
    ConnectorOptions? options, CancellationToken cancellationToken = default)
    {

    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var compressOptions = options.ToObject<CompressOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions, cancellationToken).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateJsonPerRow is false)
        {
            var rowContent = _jsonHandler.ToJson(filteredData, indentedOptions.Indented);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}{_jsonHandler.Extension}",
                ContentType =_jsonHandler.ContentType,
                Content = rowContent.ToByteArray(),
            });

            return compressEntries;
        }

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        foreach (DataRow row in filteredData.Rows)
        {
            try
            {
                var rowContent = _jsonHandler.ToJson(row, indentedOptions.Indented);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{Guid.NewGuid().ToString()}{_jsonHandler.Extension}",
                    ContentType = _jsonHandler.ContentType,
                    Content = rowContent.ToByteArray(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                continue;
            }
        }

        return compressEntries;
    }

    #region internal methods
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

    private object GetMetaData(object content)
    {
        var contentHash = FlowSynx.Security.HashHelper.Md5.GetHash(content);
        return new
        {
            ContentHash = contentHash
        };
    }

    private DataTable JArrayToDataTable(JToken token, bool? includeMetaData)
    {
        var result = new DataTable();
        foreach (var row in token)
        {
            var dict = _jsonHandler.Flatten(row);
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
        var dict = _jsonHandler.Flatten(token);

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
        var dict = _jsonHandler.Flatten(token);

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

    private dynamic PrepareDataForWrite(WriteOptions writeOptions, IndentedOptions indentedOptions,
        object dataOptions)
    {
        var dataValue = dataOptions.GetObjectValue();

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

    private Task WriteEntityAsync(string entity, string content, bool append,
    CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        using (StreamWriter writer = new StreamWriter(path, append))
        {
            writer.WriteLine(content);
        }

        return Task.CompletedTask;
    }

    private async Task<ReadResult> ReadEntityAsync(string content, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        var entities = await FilteredEntitiesAsync(content, listOptions, cancellationToken)
                            .ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = _jsonHandler.ToJson(entities, true).ToByteArray() }
        };
    }

    private async Task<string> ReadContent(string entity, Connector? connector, 
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (connector is not null)
        {
            var content = await connector.ReadAsync(new Context(path), null, cancellationToken);
            return Encoding.UTF8.GetString(content.Content);
        }
        else
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
    }

    private async Task<DataTable> FilteredEntitiesAsync(string content, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        var dataTable = await EntitiesAsync(content, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        return _dataFilter.Filter(dataTable, dataFilterOptions);
    }

    private Task<DataTable> EntitiesAsync(string json, ListOptions options,
        CancellationToken cancellationToken = new CancellationToken())
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
    #endregion
}