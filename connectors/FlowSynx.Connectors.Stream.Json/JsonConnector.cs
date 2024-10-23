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
using FlowSynx.Connectors.Stream.Json.Models;
using FlowSynx.Connectors.Stream.Json.Services;

namespace FlowSynx.Connectors.Stream.Json;

public class JsonConnector : Connector
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private readonly IDataFilter _dataFilter;
    private readonly IJsonManager _jsonManager;

    public JsonConnector(ILogger<JsonConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _serializer = serializer;
        _dataFilter = dataFilter;
        _jsonManager = new JsonManager(serializer);
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

        var append = writeOptions.OverWrite is false;
        await WriteEntityAsync(context.Entity, content, append).ConfigureAwait(false);
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
        var filteredData = await FilteredEntitiesAsync(content, listOptions).ConfigureAwait(false);

        return filteredData.Rows.Count > 0;
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions).ConfigureAwait(false);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        if (destinationConnector is null)
            throw new StreamException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var transferOptions = options.ToObject<TransferOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var transferData = await PrepareTransferring(sourceContext, listOptions, transferOptions, indentedOptions, cancellationToken);

        await destinationConnector.ProcessTransferAsync(destinationContext, transferData, options, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);

        var transferOptions = options.ToObject<TransferOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

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

                    var data = _jsonManager.ToJson(newRow, indentedOptions.Indented);
                    var clonedContext = (Context)context.Clone();
                    var newPath = transferData.Namespace == Namespace.Storage
                        ? row.Key
                        : PathHelper.Combine(path, row.Key);

                    if (Path.GetExtension(newPath) != _jsonManager.Extension)
                    {
                        _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                           $"So its extension will be automatically changed to {_jsonManager.Extension}");

                        newPath = Path.ChangeExtension(path, _jsonManager.Extension);
                    }

                    clonedContext.Entity = Path.ChangeExtension(newPath, _jsonManager.Extension);

                    await WriteAsync(clonedContext, options, data, cancellationToken);
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

            var data = _jsonManager.ToJson(dataTable, indentedOptions.Indented);
            var clonedContext = (Context)context.Clone();

            var newPath = path;
            if (Path.GetExtension(path) != _jsonManager.Extension)
            {
                _logger.LogWarning($"The target path '{newPath}' is not ended with json extension. " +
                                   $"So its extension will be automatically changed to {_jsonManager.Extension}");

                newPath = Path.ChangeExtension(path, _jsonManager.Extension);
            }
            clonedContext.Entity = newPath;

            await WriteAsync(clonedContext, options, data, cancellationToken);
        }
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
        var filteredData = await FilteredEntitiesAsync(content, listOptions).ConfigureAwait(false);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateJsonPerRow is false)
        {
            var rowContent = _jsonManager.ToJson(filteredData, indentedOptions.Indented);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}{_jsonManager.Extension}",
                ContentType =_jsonManager.ContentType,
                Content = rowContent.ToByteArray(),
            });

            return compressEntries;
        }

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        foreach (DataRow row in filteredData.Rows)
        {
            try
            {
                var rowContent = _jsonManager.ToJson(row, indentedOptions.Indented);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{Guid.NewGuid().ToString()}{_jsonManager.Extension}",
                    ContentType = _jsonManager.ContentType,
                    Content = rowContent.ToByteArray(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
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
        var contentHash = Security.HashHelper.Md5.GetHash(content);
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
            var dict = _jsonManager.Flatten(row);
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
        var dict = _jsonManager.Flatten(token);

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
        var dict = _jsonManager.Flatten(token);

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

    private Task WriteEntityAsync(string entity, string content, bool append)
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

    private async Task<ReadResult> ReadEntityAsync(string content, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        var entities = await FilteredEntitiesAsync(content, listOptions)
                            .ConfigureAwait(false);

        return entities.Rows.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = _jsonManager.ToJson(entities, true).ToByteArray() }
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

    private async Task<DataTable> FilteredEntitiesAsync(string content, ListOptions listOptions)
    {
        var dataTable = await EntitiesAsync(content, listOptions);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        return _dataFilter.Filter(dataTable, dataFilterOptions);
    }

    private Task<DataTable> EntitiesAsync(string json, ListOptions options)
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

    private async Task<TransferData> PrepareTransferring(Context context, ListOptions listOptions,
        TransferOptions transferOptions, IndentedOptions indentedOptions,
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(context.Entity);

        var content = await ReadContent(path, context.Connector, cancellationToken);
        var filteredData = await FilteredEntitiesAsync(content, listOptions).ConfigureAwait(false);

        var isSeparateJsonPerRow = transferOptions.SeparateJsonPerRow;
        var jsonContentBase64 = string.Empty;

        var transferKind = GetTransferKind(transferOptions.TransferKind);
        if (!isSeparateJsonPerRow)
        {
            var jsonContent = _jsonManager.ToJson(filteredData, indentedOptions.Indented);
            jsonContentBase64 = jsonContent.ToBase64String();
        }

        var result = new TransferData
        {
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = transferKind,
            ContentType = isSeparateJsonPerRow ? string.Empty : _jsonManager.ContentType,
            Content = isSeparateJsonPerRow ? string.Empty : jsonContentBase64,
            Columns = _jsonManager.GetColumnNames(filteredData),
            Rows = _jsonManager.GenerateTransferDataRow(filteredData, indentedOptions.Indented)
        };

        return result;
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
    #endregion
}