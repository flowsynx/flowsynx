using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions;
using Newtonsoft.Json.Linq;
using System.Data;
using FlowSynx.IO;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Data.Extensions;
using FlowSynx.Plugin.Stream.Exceptions;
using Newtonsoft.Json;
using System.Text;
using FlowSynx.Plugin.Stream.Json.Options;

namespace FlowSynx.Plugin.Stream.Json;

public class JsonStream : PluginBase
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private readonly IDataFilter _dataFilter;
    private readonly JsonHandler _jsonHandler;
    private const string ContentType = "application/json";
    private const string Extension = ".json";
    public JsonStream(ILogger<JsonStream> logger, IDataFilter dataFilter,
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
    public override PluginNamespace Namespace => PluginNamespace.Stream;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(JsonStreamSpecifications);

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (inferiorPlugin is not null)
        {
            await inferiorPlugin.WriteAsync(path, null, options, string.Empty, cancellationToken);
            return;
        }

        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && createOptions.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        File.Create(path).Dispose();
    }

    public override async Task WriteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (inferiorPlugin is not null)
        {
            await inferiorPlugin.WriteAsync(path, null, options, dataOptions, cancellationToken);
            return;
        }

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var writeOptions = options.ToObject<WriteOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

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
        
        File.WriteAllText(path, serializeData);
    }

    public override async Task<ReadResult> ReadAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var content = await ReadContent(entity, inferiorPlugin, cancellationToken);

        var dataTable = await ListEntitiesAsync(content, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return result.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => new ReadResult { Content = ObjectToByteArray(result.First()) }
        };
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

    public override Task UpdateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<bool> ExistAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var content = await ReadContent(entity, inferiorPlugin, cancellationToken);

        var dataTable = await ListEntitiesAsync(content, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();
        return result.Any();
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var content = await ReadContent(path, inferiorPlugin, cancellationToken);

        var dataTable = await ListEntitiesAsync(content, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    public override async Task<TransferData> PrepareTransferring(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var transferOptions = options.ToObject<TransferOptions>();
        var transferDataRows = new List<TransferDataRow>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var transferKind = GetTransferKind(transferOptions.TransferKind);
        var readContent = await ReadContent(entity, inferiorPlugin, cancellationToken);

        var dataTable = await ListEntitiesAsync(readContent, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        var isSeparateJsonPerRow = transferOptions.SeparateJsonPerRow is true;
        var jsonContentBase64 = string.Empty;

        if (!isSeparateJsonPerRow)
        {
            var jsonContent = _jsonHandler.ToJson(filteredData, indentedOptions.Indented);
            jsonContentBase64 = jsonContent.ToBase64String();
        }

        foreach (DataRow row in filteredData.Rows)
        {
            var itemArray = row.ItemArray;
            var content = _jsonHandler.ToJson(row, indentedOptions.Indented);
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
            Kind = transferKind,
            ContentType = isSeparateJsonPerRow ? string.Empty : ContentType,
            Content = isSeparateJsonPerRow ? string.Empty : jsonContentBase64,
            Columns = filteredData.Columns.Cast<DataColumn>().Select(x => x.ColumnName),
            Rows = transferDataRows
        };

        return result;
    }

    public override Task TransferAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, TransferData transferData, 
        CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var transferOptions = options.ToObject<TransferOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var dataTable = new DataTable();
        foreach (var column in transferData.Columns)
        {
            dataTable.Columns.Add(column);
        }

        if (transferOptions.SeparateJsonPerRow is true)
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
                    File.WriteAllText(PathHelper.Combine(path, row.Key), _jsonHandler.ToJson(newRow, indentedOptions.Indented));
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

            File.WriteAllText(path, _jsonHandler.ToJson(dataTable, indentedOptions.Indented));
        }

        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var listOptions = options.ToObject<ListOptions>();
        var compressOptions = options.ToObject<CompressOptions>();
        var indentedOptions = options.ToObject<IndentedOptions>();

        var readContent = await ReadContent(entity, inferiorPlugin, cancellationToken);

        var dataTable = await ListEntitiesAsync(readContent, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);

        if (filteredData.Rows.Count <= 0)
            throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();

        if (compressOptions.SeparateJsonPerRow is false)
        {
            var content = _jsonHandler.ToJson(filteredData, indentedOptions.Indented);
            compressEntries.Add(new CompressEntry
            {
                Name = $"{Guid.NewGuid().ToString()}{Extension}",
                ContentType = ContentType,
                Content = StringToByteArray(content),
            });

            return compressEntries;
        }

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        foreach (DataRow row in filteredData.Rows)
        {
            try
            {
                var content = _jsonHandler.ToJson(row, indentedOptions.Indented);
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

        return compressEntries;
    }

    #region internal methods
    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
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

    private async Task<string> ReadContent(string entity, PluginBase? plugin, 
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (plugin is not null)
        {
            var content = await plugin.ReadAsync(path, null, null, cancellationToken);
            return Encoding.UTF8.GetString(content.Content);
        }
        else
        {
            return File.ReadAllText(path);
        }
    }

    private Task<DataTable> ListEntitiesAsync(string json, ListOptions options,
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

    private byte[] StringToByteArray(string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }
    #endregion
}