﻿namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowTaskExecutionEntity: AuditableEntity<Guid>, ISoftDeletable
{
    public required string Name { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public WorkflowTaskExecutionStatus Status { get; set; } = WorkflowTaskExecutionStatus.Pending;
    public string? Message { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsDeleted { get; set; } = false;

    public WorkflowExecutionEntity WorkflowExecution { get; set; }
}