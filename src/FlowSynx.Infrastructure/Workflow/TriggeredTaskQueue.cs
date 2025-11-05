using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSynx.Infrastructure.Workflow;

public class TriggeredTaskQueue : ITriggeredTaskQueue
{
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _queues = new();

    public void Enqueue(Guid workflowExecutionId, string taskName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        var queue = _queues.GetOrAdd(workflowExecutionId, _ => new ConcurrentQueue<string>());
        queue.Enqueue(taskName);
    }

    public bool TryDequeue(Guid workflowExecutionId, out string taskName)
    {
        taskName = default!;
        if (!_queues.TryGetValue(workflowExecutionId, out var queue))
            return false;

        return queue.TryDequeue(out taskName!);
    }

    public void Clear(Guid workflowExecutionId)
    {
        _queues.TryRemove(workflowExecutionId, out _);
    }
}