using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionTasks;

public class WorkflowExecutionTasksResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid WorkflowId { get; set; }
    public required Guid WorkflowExecutionId { get; set; }
    public WorkflowTaskExecutionStatus Status { get; set; } = WorkflowTaskExecutionStatus.Pending;
    public string? Message { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}