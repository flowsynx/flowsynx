namespace FlowSynx.Infrastructure.Workflow;

public interface ITriggeredTaskQueue
{
    void Enqueue(Guid workflowExecutionId, string taskName);
    bool TryDequeue(Guid workflowExecutionId, out string taskName);
    bool Contains(Guid workflowExecutionId, string taskName);
    void Clear(Guid workflowExecutionId);
}