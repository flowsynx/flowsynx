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
using SharpCompress.Common;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace FlowSynx.Plugin.Stream.Json;

public class JsonStream : PluginBase
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private readonly IDataFilter _dataFilter;
    private readonly JsonHandler _jsonHandler;

    public JsonStream(ILogger<JsonStream> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _serializer = serializer;
        _dataFilter = dataFilter;
        _jsonHandler = new JsonHandler();
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

    public override Task<object> About(PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public override Task CreateAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && createOptions.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        File.Create(path).Dispose();

        return Task.CompletedTask;
    }

    public override Task WriteAsync(string entity, PluginOptions? options, object dataOptions, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

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
            Indented = writeOptions.Indented ?? false,
        });
        
        File.WriteAllText(path, serializeData);

        return Task.CompletedTask;
    }

    public override async Task<object> ReadAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var dataTable = await ListEntitiesAsync(path, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return result.Count switch
        {
            <= 0 => throw new StreamException(string.Format(Resources.NoItemsFoundWithTheGivenFilter, path)),
            > 1 => throw new StreamException(Resources.FilteringDataMustReturnASingleItem),
            _ => result.First()
        };
    }

    public override Task UpdateAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;

        var dataTable = await ListEntitiesAsync(path, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        _jsonHandler.Delete(dataTable, filteredData);

        var x = GetDict(dataTable);
        var unFlatten = _jsonHandler.UnFlattenJson(x);
        var data = _serializer.Serialize(unFlatten);
        //var result = filteredData.CreateListFromTable();
        //var data = _csvHandler.ToCsv(dataTable, delimiter);
        await File.WriteAllTextAsync(path, data, cancellationToken);
    }

    public override async Task<bool> ExistAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var dataTable = await ListEntitiesAsync(path, listOptions, cancellationToken);
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();
        return result.Any();
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var dataTable = await ListEntitiesAsync(path, listOptions, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return result;
    }

    public override Task<TransferData> PrepareTransferring(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task TransferAsync(string entity, PluginOptions? options, TransferData transferData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    private Task<DataTable> ListEntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var json = File.ReadAllText(path);
        var jToken = JToken.Parse(json);

        var dataTable = jToken switch
        {
            JArray => JArrayToDataTable(jToken, options.IncludeMetadata),
            JObject => JObjectToDataTable(jToken, options.IncludeMetadata),
            _ => JPropertyToDataTable(jToken, options.IncludeMetadata)
        };

        return Task.FromResult(dataTable);
    }

    private Dictionary<string, string> GetDict(DataTable dt)
    {
        return dt.AsEnumerable()
            .ToDictionary<DataRow, string, string>(row => row.Field<string>(0),
                row => row.Field<string>(1));
    }
    #endregion
}