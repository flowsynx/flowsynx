namespace FlowSynx.Infrastructure.Workflow;

public class CancellationRegistryKey : IEquatable<CancellationRegistryKey>
{
    public string UserId { get; }
    public Guid WorkflowId { get; }
    public Guid WorkflowExecutionId { get; }

    public CancellationRegistryKey(string userId, Guid workflowId, Guid workflowExecutionId)
    {
        UserId = userId;
        WorkflowId = workflowId;
        WorkflowExecutionId = workflowExecutionId;
    }

    public override bool Equals(object obj) => 
        obj is CancellationRegistryKey other && Equals(other);

    public bool Equals(CancellationRegistryKey? other) => 
        UserId == other?.UserId && WorkflowId == other?.WorkflowId && WorkflowExecutionId == other.WorkflowExecutionId;

    public override int GetHashCode() => 
        HashCode.Combine(WorkflowId, WorkflowExecutionId);
}