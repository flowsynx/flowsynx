using System.Collections.ObjectModel;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Enumerates the types of data mutations supported by the data-based trigger pipeline.
/// </summary>
public enum DataChangeOperation
{
    Insert,
    Update,
    Delete
}

/// <summary>
/// Represents a single change that occurred in a data source and may trigger a workflow execution.
/// </summary>
public sealed class DataChangeEvent
{
    public DataChangeEvent(
        string source,
        string table,
        DataChangeOperation operation,
        object? primaryKey,
        IReadOnlyDictionary<string, object?>? currentValues,
        IReadOnlyDictionary<string, object?>? previousValues,
        DateTimeOffset timestamp,
        string? cursor = null)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table cannot be null or whitespace.", nameof(table));

        Source = source;
        Table = table;
        Operation = operation;
        PrimaryKey = primaryKey;
        CurrentValues = currentValues != null
            ? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(currentValues, StringComparer.OrdinalIgnoreCase))
            : null;
        PreviousValues = previousValues != null
            ? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(previousValues, StringComparer.OrdinalIgnoreCase))
            : null;
        Timestamp = timestamp;
        Cursor = cursor;
    }

    /// <summary>
    /// Gets the data provider that produced this change notification.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the logical table or collection the change originated from.
    /// </summary>
    public string Table { get; }

    /// <summary>
    /// Gets the type of mutation that occurred.
    /// </summary>
    public DataChangeOperation Operation { get; }

    /// <summary>
    /// Gets the primary key associated with the change, if available.
    /// </summary>
    public object? PrimaryKey { get; }

    /// <summary>
    /// Gets the values after the change was applied (if provided by the provider).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? CurrentValues { get; }

    /// <summary>
    /// Gets the values before the change was applied (if provided by the provider).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? PreviousValues { get; }

    /// <summary>
    /// Gets the timestamp reported by the provider for this change.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets an opaque cursor or sequence value that can be used for incremental polling.
    /// </summary>
    public string? Cursor { get; }

    /// <summary>
    /// Builds a serializable payload that matches the event contract expected by workflow tasks.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ToPayload(IDictionary<string, object?>? additional = null)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["table"] = Table,
            ["operation"] = Operation.ToString(),
            ["primaryKey"] = PrimaryKey,
            ["timestamp"] = Timestamp.UtcDateTime,
            ["source"] = Source
        };

        if (!string.IsNullOrWhiteSpace(Cursor))
            payload["cursor"] = Cursor;

        var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (PreviousValues != null && PreviousValues.Count > 0)
            diff["previous"] = PreviousValues;
        if (CurrentValues != null && CurrentValues.Count > 0)
            diff["current"] = CurrentValues;

        if (diff.Count > 0)
            payload["diff"] = diff;

        if (additional != null)
        {
            foreach (var pair in additional)
            {
                payload[pair.Key] = pair.Value;
            }
        }

        return payload;
    }
}
