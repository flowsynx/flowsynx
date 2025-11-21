using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

public class WorkflowTaskExecutionLogsRequest : IRequest<PaginatedResult<WorkflowTaskExecutionLogsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public required string WorkflowTaskExecutionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
