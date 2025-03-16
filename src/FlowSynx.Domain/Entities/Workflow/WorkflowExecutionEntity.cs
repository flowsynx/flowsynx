namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowExecutionEntity : AuditableEntity<Guid>
{
    public required Guid WorkflowId { get; set; }
    public required string UserId { get; set; }
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Pending;
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }

    public WorkflowEntity Workflow { get; set; }
    public List<WorkflowTaskExecutionEntity> TaskExecutions { get; set; } = new();
}
