using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Captures the configuration payload stored on a <see cref="WorkflowTriggerEntity"/> for data-based triggers.
/// </summary>
public sealed class DataTriggerConfiguration
{
    private readonly HashSet<string> _tables;
    private readonly HashSet<DataChangeOperation> _operations;
    private readonly HashSet<string> _columns;

    private DataTriggerConfiguration(
        WorkflowTriggerEntity trigger,
        string providerKey,
        IEnumerable<string> tables,
        IEnumerable<DataChangeOperation> operations,
        IEnumerable<string> columns,
        TimeSpan pollInterval,
        IReadOnlyDictionary<string, object> providerSettings)
    {
        Trigger = trigger;
        ProviderKey = providerKey;
        PollInterval = pollInterval;
        _tables = new HashSet<string>(tables, StringComparer.OrdinalIgnoreCase);
        _operations = new HashSet<DataChangeOperation>(operations);
        _columns = new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
        ProviderSettings = providerSettings;
    }

    public WorkflowTriggerEntity Trigger { get; }
    public Guid TriggerId => Trigger.Id;
    public Guid WorkflowId => Trigger.WorkflowId;
    public string UserId => Trigger.UserId;
    public string ProviderKey { get; }
    public TimeSpan PollInterval { get; }
    public IReadOnlyCollection<string> Tables => new ReadOnlyCollection<string>(_tables.ToList());
    public IReadOnlyCollection<DataChangeOperation> Operations => new ReadOnlyCollection<DataChangeOperation>(_operations.ToList());
    public IReadOnlyCollection<string> Columns => new ReadOnlyCollection<string>(_columns.ToList());
    public IReadOnlyDictionary<string, object> ProviderSettings { get; }

    public bool MatchesTable(string table) => _tables.Count == 0 || _tables.Contains(table);

    public bool MatchesOperation(DataChangeOperation operation) =>
        _operations.Count == 0 || _operations.Contains(operation);

    public static bool TryCreate(
        WorkflowTriggerEntity trigger,
        out DataTriggerConfiguration? configuration,
        out string? error)
    {
        configuration = null;
        error = null;

        var properties = trigger.Properties ?? new Dictionary<string, object>();

        var provider = ReadString(properties, "provider");
        if (string.IsNullOrWhiteSpace(provider))
        {
            error = "Provider setting (provider) is required.";
            return false;
        }

        var providerKey = provider.Trim().ToUpperInvariant();
        var tables = ReadStringArray(properties, "tables");
        var operations = ReadOperations(properties);
        var columns = ReadStringArray(properties, "columns");
        var pollInterval = ReadPollInterval(properties);
        var providerSettings = ReadDictionary(properties, "settings");

        configuration = new DataTriggerConfiguration(
            trigger,
            providerKey,
            tables,
            operations,
            columns,
            pollInterval,
            providerSettings);
        return true;
    }

    private static TimeSpan ReadPollInterval(IReadOnlyDictionary<string, object> properties)
    {
        var pollIntervalSeconds = ReadInt(properties, "pollIntervalSec");
        if (pollIntervalSeconds <= 0)
        {
            pollIntervalSeconds = 30;
        }

        return TimeSpan.FromSeconds(pollIntervalSeconds);
    }

    private static string? ReadString(IReadOnlyDictionary<string, object> properties, string key)
    {
        if (!properties.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            string str => str,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String => jsonElement.GetString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static IReadOnlyCollection<string> ReadStringArray(IReadOnlyDictionary<string, object> properties, string key)
    {
        if (!properties.TryGetValue(key, out var value) || value is null)
            return Array.Empty<string>();

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement
                    .EnumerateArray()
                    .Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim())
                    .ToArray();
            }

            if (jsonElement.ValueKind == JsonValueKind.String)
                return new[] { jsonElement.GetString() ?? string.Empty };
        }

        if (value is IEnumerable<object?> objectEnumerable && value is not string)
        {
            return objectEnumerable
                .Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToArray();
        }

        var converted = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(converted)
            ? Array.Empty<string>()
            : new[] { converted.Trim() };
    }

    private static IReadOnlyCollection<DataChangeOperation> ReadOperations(IReadOnlyDictionary<string, object> properties)
    {
        var tokens = ReadStringArray(properties, "events");
        if (tokens.Count == 0)
            return Enum.GetValues<DataChangeOperation>();

        var operations = new List<DataChangeOperation>();
        foreach (var token in tokens)
        {
            if (Enum.TryParse<DataChangeOperation>(token, true, out var operation))
            {
                operations.Add(operation);
            }
        }

        return operations.Count == 0 ? Enum.GetValues<DataChangeOperation>() : operations;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object> properties, string key)
    {
        if (!properties.TryGetValue(key, out var rawValue) || rawValue is null)
            return 0;

        return rawValue switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetInt32(),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static IReadOnlyDictionary<string, object> ReadDictionary(
        IReadOnlyDictionary<string, object> properties,
        string key)
    {
        if (!properties.TryGetValue(key, out var value) || value is null)
            return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

        var dictionary = ConvertToObject(value) as IDictionary<string, object?>;
        if (dictionary == null)
            return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

        var normalized = dictionary
            .Where(pair => pair.Key != null)
            .ToDictionary(pair => pair.Key!, pair => (object?)pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return new ReadOnlyDictionary<string, object>(normalized);
    }

    private static object? ConvertToObject(object value)
    {
        return value switch
        {
            JsonElement element => ConvertJsonElement(element),
            IDictionary<string, object?> dictionary => dictionary.ToDictionary(
                pair => pair.Key,
                pair => ConvertToObject(pair.Value ?? string.Empty),
                StringComparer.OrdinalIgnoreCase),
            IEnumerable<object?> enumerable when value is not string => enumerable.Select(ConvertToObject).ToList(),
            _ => value
        };
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    result[property.Name] = ConvertJsonElement(property.Value);
                }
                return result;
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(ConvertJsonElement).ToList();
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            default:
                return element.GetRawText();
        }
    }
}
