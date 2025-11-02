using FlowSynx.Application.Serialization;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Serialization;

public class JsonSanitizer : IJsonSanitizer
{
    public string Sanitize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        return TrailingCommaRegex().Replace(json, "$1");
    }

    private static Regex TrailingCommaRegex() => new Regex(@",\s*(\]|\})", RegexOptions.Compiled);
}
