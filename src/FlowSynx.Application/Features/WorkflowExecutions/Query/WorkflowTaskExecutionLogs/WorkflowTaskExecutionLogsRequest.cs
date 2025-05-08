using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

public class WorkflowTaskExecutionLogsRequest : IRequest<Result<IEnumerable<WorkflowTaskExecutionLogsResponse>>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public required string WorkflowTaskExecutionId { get; set; }
}