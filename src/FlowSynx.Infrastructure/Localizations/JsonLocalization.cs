using FlowSynx.Application.Localizations;
using FlowSynx.Application.Serialization;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Localizations;

public class JsonLocalization : ILocalization
{
    private readonly Language _language;
    private readonly ILogger<JsonLocalization> _logger;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly Dictionary<string, Dictionary<string, string>> _localizations;
    private static readonly Regex PlaceholderRegex = new(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    public JsonLocalization(
        Language language,
        ILogger<JsonLocalization> logger, 
        IJsonDeserializer jsonDeserializer)
    {
        _language = language;
        _logger = logger;
        _jsonDeserializer = jsonDeserializer;
        _localizations = LoadEmbeddedLocalizations();
    }

    public string Get(string key)
    {
        return GetRaw(key);
    }

    public string Get(string key, params object[] args)
    {
        var template = GetRaw(key);

        var placeholderNames = ExtractPlaceholdersInOrder(template).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (placeholderNames.Count != args.Length)
        {
            _logger.LogError("Localization key '{Key}' expects {Expected} args, but got {Actual}. Template: {Template}",
                key, placeholderNames.Count, args.Length, template);
            throw new FormatException($"Expected {placeholderNames.Count} arguments but got {args.Length} for key '{key}'");
        }

        for (int i = 0; i < placeholderNames.Count; i++)
        {
            string placeholder = "{" + placeholderNames[i] + "}";
            template = template.Replace(placeholder, args[i]?.ToString() ?? string.Empty);
        }

        return template;
    }

    private string GetRaw(string key)
    {
        if (_localizations.TryGetValue(_language.Code, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        _logger.LogWarning("Missing localization for key '{Key}' in culture '{Culture}'", key, _language.Name);
        return key;
    }

    private static List<string> ExtractPlaceholdersInOrder(string template)
    {
        return PlaceholderRegex.Matches(template)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private Dictionary<string, Dictionary<string, string>> LoadEmbeddedLocalizations()
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames()
                         .Where(r => r.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                var culture = Path.GetFileNameWithoutExtension(resourceName).Split('.').Last().ToLowerInvariant();

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var parsed = _jsonDeserializer.Deserialize<Dictionary<string, string>>(json);
                if (parsed == null) continue;

                if (!result.TryGetValue(culture, out var dict))
                    dict = result[culture] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var kv in parsed)
                {
                    if (!dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] = kv.Value;
                    }
                    else
                    {
                        // Optionally log key conflict or override
                        // _logger.LogWarning("Duplicate localization key '{Key}' in {Resource}", kv.Key, resourceName);
                    }
                }
            }
        }

        return result;
    }
}