using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.WorkflowExecutions;

public class WorkflowExecutionLog : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid WorkflowExecutionId { get; set; }
    public string Level { get; set; } = string.Empty; // info, warn, error, debug
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public WorkflowExecution? WorkflowExecution { get; set; }
}