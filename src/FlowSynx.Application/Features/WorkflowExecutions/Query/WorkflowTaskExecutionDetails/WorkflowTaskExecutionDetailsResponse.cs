using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;

public class WorkflowTaskExecutionDetailsResponse
{
    public Guid Id { get; set; }
    public WorkflowTaskExecutionStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}