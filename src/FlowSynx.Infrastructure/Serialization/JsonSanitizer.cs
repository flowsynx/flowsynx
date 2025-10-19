using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Serialization;

internal static partial class JsonSanitizer
{
    public static string Sanitize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        return TrailingCommaRegex().Replace(json, "$1");
    }

    [GeneratedRegex(@",\s*(\]|\})", RegexOptions.Compiled)]
    private static partial Regex TrailingCommaRegex();
}
