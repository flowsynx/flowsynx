using FlowSynx.Application.Serializations;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonNormalizer : INormalizer
{
    public string Normalize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        return TrailingCommaRegex().Replace(json, "$1");
    }

    private static Regex TrailingCommaRegex() => new Regex(@",\s*(\]|\})", RegexOptions.Compiled, System.TimeSpan.FromSeconds(10));
}
