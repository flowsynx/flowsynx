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

    public JsonStream(ILogger<JsonStream> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        _logger = logger;
        _deserializer = deserializer;
        _dataFilter = dataFilter;
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
        JToken jToken = JToken.Parse(json);

        if (jToken is JArray)
        {
            var jsonLinq = JArray.Parse(json);

            // Find the first array using Linq
            var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();
            var trgArray = new JArray();
            foreach (JObject row in srcArray.Children<JObject>())
            {
                var cleanRow = new JObject();
                foreach (JProperty column in row.Properties())
                {
                    // Only include JValue types
                    if (column.Value is JValue)
                    {
                        cleanRow.Add(column.Name, column.Value);
                    }
                }

                trgArray.Add(cleanRow);
            }
        }
        //else if (jToken is JObject)
            
        else
            throw new Exception("Unable to cast json to unknown type");

        //var jsonLinq = JObject.Parse(json);

        //// Find the first array using Linq
        //var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();
        //var trgArray = new JArray();
        //foreach (JObject row in srcArray.Children<JObject>())
        //{
        //    var cleanRow = new JObject();
        //    foreach (JProperty column in row.Properties())
        //    {
        //        // Only include JValue types
        //        if (column.Value is JValue)
        //        {
        //            cleanRow.Add(column.Name, column.Value);
        //        }
        //    }

        //    trgArray.Add(cleanRow);
        //}

        //// Flatten the nested JSON object
        //JObject flattenedJsonObject = new JObject();
        //foreach (var property in nestedJsonObject.Properties())
        //{
        //    if (property.Value.Type == JTokenType.Object)
        //    {
        //        foreach (var nestedProperty in property.Value.Children<JProperty>())
        //        {
        //            flattenedJsonObject.Add(nestedProperty.Name, nestedProperty.Value);
        //        }
        //    }
        //    else
        //    {
        //        flattenedJsonObject.Add(property.Name, property.Value);
        //    }
        //}

        var listOptions = options.ToObject<ListOptions>();
        var dataFilterOptions = GetDataFilterOptions(listOptions);
        var dataTable = _deserializer.Deserialize<DataTable>(json);
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
    #endregion
}