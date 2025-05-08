using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

public class WorkflowExecutionLogsRequest : IRequest<Result<IEnumerable<WorkflowExecutionLogsResponse>>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
}