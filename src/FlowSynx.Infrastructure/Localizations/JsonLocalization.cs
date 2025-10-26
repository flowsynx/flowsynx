using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Localizations;

public class JsonLocalization : ILocalization
{
    private readonly Language _language;
    private readonly ILogger<JsonLocalization> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _localizations;
    private static readonly Regex PlaceholderRegex = new(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    public JsonLocalization(
        Language language,
        ILogger<JsonLocalization> logger)
    {
        _language = language;
        _logger = logger;
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
        var localizations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            LoadAssemblyLocalizations(assembly, localizations);
        }

        return localizations;
    }

    /// <summary>
    /// Iterates through an assembly's embedded JSON resources and merges any localization entries found.
    /// </summary>
    private void LoadAssemblyLocalizations(Assembly assembly, Dictionary<string, Dictionary<string, string>> store)
    {
        foreach (var resourceName in GetLocalizationResourceNames(assembly))
        {
            var culture = GetCultureFromResourceName(resourceName);
            if (culture == null)
                continue;

            var resourceEntries = ReadLocalizationResource(assembly, resourceName);
            if (resourceEntries == null)
                continue;

            MergeLocalizationEntries(store, culture, resourceEntries);
        }
    }

    /// <summary>
    /// Filters manifest resource names to include only potential localization JSON payloads.
    /// </summary>
    private static IEnumerable<string> GetLocalizationResourceNames(Assembly assembly)
    {
        return assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Attempts to extract a culture token from an embedded resource name.
    /// </summary>
    private static string? GetCultureFromResourceName(string resourceName)
    {
        var fileName = Path.GetFileNameWithoutExtension(resourceName);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return fileName.Split('.').LastOrDefault()?.ToLowerInvariant();
    }

    /// <summary>
    /// Reads the embedded JSON resource and deserializes it into localization key-value pairs.
    /// </summary>
    private static Dictionary<string, string>? ReadLocalizationResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    /// <summary>
    /// Merges parsed localization entries into the accumulated store while preserving existing keys.
    /// </summary>
    private void MergeLocalizationEntries(
        Dictionary<string, Dictionary<string, string>> store,
        string culture,
        Dictionary<string, string> entries)
    {
        if (!store.TryGetValue(culture, out var cultureEntries))
        {
            cultureEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            store[culture] = cultureEntries;
        }

        foreach (var kvp in entries)
        {
            if (cultureEntries.ContainsKey(kvp.Key))
                continue;

            cultureEntries[kvp.Key] = kvp.Value;
            // Optional: use _logger to highlight duplicate keys if the project chooses to enforce uniqueness.
        }
    }
}
