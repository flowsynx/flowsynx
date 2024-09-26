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

namespace FlowSynx.Plugin.Stream.Json;

public class JsonStream : PluginBase
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private readonly JsonHandler _jsonHandler;

    public JsonStream(ILogger<JsonStream> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        _logger = logger;
        _deserializer = deserializer;
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
        throw new NotImplementedException();
    }

    public override Task CreateAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task WriteAsync(string entity, PluginOptions? options, object dataOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<object> ReadAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task UpdateAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ExistAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var json = File.ReadAllText(path);
        var jToken = JToken.Parse(json);
        var listOptions = options.ToObject<ListOptions>();

        DataTable dataTable;
        switch (jToken)
        {
            case JArray:
                dataTable = JArrayToDataTable(jToken, listOptions.IncludeMetadata);
                break;
            case JObject:
                dataTable = JObjectToDataTable(jToken, listOptions.IncludeMetadata);
                break;
            default:
            {
                dataTable = new DataTable();
                dataTable.Columns.Add("Sample");
                var dataRow = dataTable.NewRow();
                dataRow["Sample"] = ((JValue)jToken).Value;
                dataTable.Rows.Add(dataRow);
                break;
            }
        }

        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return Task.FromResult<IEnumerable<object>>(result);
    }

    public override Task<TransmissionData> PrepareTransmissionData(string entity, PluginOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task TransmitDataAsync(string entity, PluginOptions? options, TransmissionData transmissionData, CancellationToken cancellationToken = default)
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
    #endregion
}