using FlowSynx.Application.Serialization;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Serialization;

public class JsonParser : IJsonParser
{
    public object? ParseObject(string json)
    {
        return JObject.Parse(json);
    }
}