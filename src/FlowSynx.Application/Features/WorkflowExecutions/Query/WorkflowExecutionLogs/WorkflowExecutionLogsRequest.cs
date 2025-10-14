using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

public class WorkflowExecutionLogsRequest : IRequest<PaginatedResult<WorkflowExecutionLogsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
