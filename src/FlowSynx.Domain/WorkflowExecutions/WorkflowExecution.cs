using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.WorkflowExecutions;

public class WorkflowExecution : AuditableEntity<Guid>, IAggregateRoot
{
    public string ExecutionId { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty; // activity, workflow, application
    public Guid TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;

    public Dictionary<string, object> Request { get; set; } = new();
    public Dictionary<string, object> Response { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public string Status { get; set; } = string.Empty; // pending, running, completed, failed, cancelled
    public int Progress { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long DurationMilliseconds { get; set; }

    public string TriggeredBy { get; set; } = string.Empty;

    public ICollection<WorkflowExecutionLog> Logs { get; set; } = new List<WorkflowExecutionLog>();
    public ICollection<WorkflowExecutionArtifact> Artifacts { get; set; } = new List<WorkflowExecutionArtifact>();
}