namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowTaskExecutionEntity: AuditableEntity<Guid>
{
    public required string Name { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public WorkflowTaskExecutionStatus Status { get; set; } = WorkflowTaskExecutionStatus.Pending;
    public string? Message { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public WorkflowExecutionEntity WorkflowExecution { get; set; }
}