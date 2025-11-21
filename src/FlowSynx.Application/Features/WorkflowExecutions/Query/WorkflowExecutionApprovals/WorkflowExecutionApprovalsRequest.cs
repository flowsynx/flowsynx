using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;

public class WorkflowExecutionApprovalsRequest : IRequest<PaginatedResult<WorkflowExecutionApprovalsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
