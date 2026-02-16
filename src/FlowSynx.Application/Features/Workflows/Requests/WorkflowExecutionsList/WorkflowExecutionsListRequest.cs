using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowExecutionsList;

public class WorkflowExecutionsListRequest : IAction<PaginatedResult<WorkflowExecutionsListResult>>
{
    public Guid WorkflowId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
