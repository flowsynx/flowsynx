using FlowSynx.Application.Services;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Services;

public class JsonExtractor : IJsonExtractor
{
    private readonly ILogger<JsonSerializer> _logger;

    public JsonExtractor(ILogger<JsonSerializer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string ExtractorArray(string json, string key)
    {
        return ExtractJson(json, key, JsonObjectType.JsonObject);
    }

    public string ExtractorObject(string json, string key)
    {
        return ExtractJson(json, key, JsonObjectType.JsonArray);
    }

    private string ExtractJson(string json, string key, JsonObjectType type)
    {
        var startIndex = json.IndexOf(key, StringComparison.Ordinal);
        if (startIndex == -1)
            return string.Empty;

        startIndex = json.IndexOf(type.StartBracket, startIndex);

        int endIndex = FindClosingBracket(json, startIndex, type);

        return json.Substring(startIndex, endIndex - startIndex + 1);
    }

    private int FindClosingBracket(string json, int startIndex, JsonObjectType type)
    {
        var braceCount = 0;
        for (var i = startIndex; i < json.Length; i++)
        {
            if (json[i] == type.StartBracket) braceCount++;
            else if (json[i] == type.EndBracket) braceCount--;

            if (braceCount == 0)
                return i;
        }
        return -1;
    }

    internal class JsonObjectType
    {
        public char StartBracket { get; set; }
        public char EndBracket { get; set; }

        public static JsonObjectType JsonObject => new JsonObjectType()
        {
            StartBracket = '{',
            EndBracket = '}'
        };

        public static JsonObjectType JsonArray => new JsonObjectType()
        {
            StartBracket = '[',
            EndBracket = ']'
        };
    }
}