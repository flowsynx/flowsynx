using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Simple polling provider that reads change events from a JSON file. The provider is primarily
/// intended for development and testing scenarios and showcases the contract used by workflow tasks.
/// </summary>
public class JsonDataChangeProvider : IDataChangeProvider
{
    public const string ProviderName = "JSON";

    private readonly ILogger<JsonDataChangeProvider> _logger;
    private readonly ILocalization _localization;

    public JsonDataChangeProvider(ILogger<JsonDataChangeProvider> logger, ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public string ProviderKey => ProviderName;

    public async Task<IReadOnlyCollection<DataChangeEvent>> GetChangesAsync(
        DataTriggerConfiguration configuration,
        DataTriggerState state,
        CancellationToken cancellationToken)
    {
        if (!configuration.ProviderSettings.TryGetValue("sourcePath", out var sourceValue) ||
            sourceValue is not string sourcePath || string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.LogWarning(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonSourceMissing", configuration.TriggerId));
            return Array.Empty<DataChangeEvent>();
        }

        var absolutePath = Path.IsPathRooted(sourcePath)
            ? sourcePath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, sourcePath));

        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonSourceMissingFile", configuration.TriggerId, absolutePath));
            return Array.Empty<DataChangeEvent>();
        }

        try
        {
            await using var stream = File.OpenRead(absolutePath);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning(_localization.Get(
                    "Workflow_DataBased_TriggerProcessor_JsonFormatInvalid", configuration.TriggerId));
                return Array.Empty<DataChangeEvent>();
            }

            var changes = new List<DataChangeEvent>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var change = TryCreateEvent(element, configuration);
                if (change == null)
                    continue;

                if (state.Cursor != null && change.Cursor != null &&
                    string.CompareOrdinal(change.Cursor, state.Cursor) <= 0)
                {
                    continue;
                }

                if (state.Cursor == null && state.LastEventTimestamp.HasValue &&
                    change.Timestamp <= state.LastEventTimestamp.Value)
                {
                    continue;
                }

                if (!configuration.MatchesTable(change.Table) || !configuration.MatchesOperation(change.Operation))
                    continue;

                changes.Add(change);
            }

            return changes;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, _localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonFormatInvalid", configuration.TriggerId));
            return Array.Empty<DataChangeEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonReadFailed", configuration.TriggerId, ex.Message));
            return Array.Empty<DataChangeEvent>();
        }
    }

    private DataChangeEvent? TryCreateEvent(JsonElement element, DataTriggerConfiguration configuration)
    {
        var table = GetString(element, "table");
        var operationToken = GetString(element, "operation");
        var timestampToken = GetString(element, "timestamp");

        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(operationToken) || string.IsNullOrWhiteSpace(timestampToken))
        {
            _logger.LogWarning(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonEventInvalid", configuration.TriggerId));
            return null;
        }

        if (!Enum.TryParse<DataChangeOperation>(operationToken, true, out var operation))
        {
            _logger.LogWarning(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonEventInvalid", configuration.TriggerId));
            return null;
        }

        if (!DateTimeOffset.TryParse(timestampToken, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var timestamp))
        {
            _logger.LogWarning(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_JsonEventInvalid", configuration.TriggerId));
            return null;
        }

        var source = GetString(element, "source") ?? configuration.ProviderKey;
        var primaryKey = GetElement(element, "primaryKey");
        var cursor = GetString(element, "cursor");

        var diffElement = TryGetProperty(element, "diff");
        IReadOnlyDictionary<string, object?>? current = null;
        IReadOnlyDictionary<string, object?>? previous = null;

        if (diffElement.HasValue && diffElement.Value.ValueKind == JsonValueKind.Object)
        {
            if (diffElement.Value.TryGetProperty("current", out var currentElement))
                current = ConvertObject(currentElement) as IReadOnlyDictionary<string, object?>;

            if (diffElement.Value.TryGetProperty("previous", out var previousElement))
                previous = ConvertObject(previousElement) as IReadOnlyDictionary<string, object?>;
        }
        else
        {
            if (element.TryGetProperty("current", out var currentElement))
                current = ConvertObject(currentElement) as IReadOnlyDictionary<string, object?>;

            if (element.TryGetProperty("previous", out var previousElement))
                previous = ConvertObject(previousElement) as IReadOnlyDictionary<string, object?>;
        }

        if (string.IsNullOrEmpty(cursor) && primaryKey != null)
        {
            cursor = $"{timestamp:O}:{primaryKey}";
        }

        return new DataChangeEvent(
            source,
            table,
            operation,
            primaryKey,
            current,
            previous,
            timestamp,
            cursor);
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static JsonElement? TryGetProperty(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? value : null;
    }

    private static object? GetElement(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? ConvertJsonElement(value) : null;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static IReadOnlyDictionary<string, object?>? ConvertObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertJsonElement(property.Value);
        }
        return dictionary;
    }
}
