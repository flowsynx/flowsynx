using FlowSynx.Application.Core.Serializations;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonObjectParser : IObjectParser
{
    public object? ParseObject(string json)
    {
        return JObject.Parse(json);
    }
}