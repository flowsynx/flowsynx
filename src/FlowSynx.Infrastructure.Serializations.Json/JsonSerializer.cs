using FlowSynx.Application.Core.Serializations;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        _options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        _options.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    }

    public string Serialize<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions(_options)
        {
            WriteIndented = false
        });
    }

    public string SerializePretty<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions(_options)
        {
            WriteIndented = true
        });
    }
}