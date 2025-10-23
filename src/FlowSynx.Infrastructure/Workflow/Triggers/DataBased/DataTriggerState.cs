using System.Collections.Generic;
using System.Linq;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Maintains polling state for a data-based trigger across scheduling cycles.
/// </summary>
public sealed class DataTriggerState
{
    private readonly object _sync = new();
    private DateTimeOffset _lastPolledAt = DateTimeOffset.MinValue;
    private string? _cursor;
    private DateTimeOffset? _lastEventTimestamp;

    public bool ShouldPoll(DateTimeOffset now, TimeSpan interval)
    {
        lock (_sync)
        {
            return now - _lastPolledAt >= interval;
        }
    }

    public void MarkPolled(DateTimeOffset now)
    {
        lock (_sync)
        {
            _lastPolledAt = now;
        }
    }

    public void Update(IEnumerable<DataChangeEvent> events)
    {
        lock (_sync)
        {
            foreach (var change in events.OrderBy(e => e.Timestamp))
            {
                _lastEventTimestamp = change.Timestamp;
                if (!string.IsNullOrWhiteSpace(change.Cursor))
                {
                    _cursor = change.Cursor;
                }
            }
        }
    }

    public string? Cursor
    {
        get
        {
            lock (_sync)
            {
                return _cursor;
            }
        }
    }

    public DateTimeOffset? LastEventTimestamp
    {
        get
        {
            lock (_sync)
            {
                return _lastEventTimestamp;
            }
        }
    }
}
