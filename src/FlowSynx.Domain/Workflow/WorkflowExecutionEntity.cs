namespace FlowSynx.Domain.Workflow;

public class WorkflowExecutionEntity : AuditableEntity<Guid>, ISoftDeletable
{
    public required Guid WorkflowId { get; set; }
    public required string UserId { get; set; }
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Pending;
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }
    public bool IsDeleted { get; set; } = false;

    public WorkflowEntity? Workflow { get; set; }
    public List<WorkflowTaskExecutionEntity> TaskExecutions { get; set; } = new();
}
