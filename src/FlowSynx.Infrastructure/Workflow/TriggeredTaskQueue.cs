using System.Collections.Concurrent;

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

    public bool Contains(Guid workflowExecutionId, string taskName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        if (!_queues.TryGetValue(workflowExecutionId, out var queue))
            return false;

        return queue.Contains(taskName);
    }

    public void Clear(Guid workflowExecutionId)
    {
        _queues.TryRemove(workflowExecutionId, out _);
    }
}