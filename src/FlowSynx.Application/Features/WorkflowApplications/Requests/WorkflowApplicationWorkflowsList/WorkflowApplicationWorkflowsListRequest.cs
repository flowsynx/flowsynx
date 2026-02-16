using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationWorkflowsList;

public class WorkflowApplicationWorkflowsListRequest : IAction<PaginatedResult<WorkflowApplicationWorkflowsListResult>>
{
    public Guid WorkflowApplicationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
