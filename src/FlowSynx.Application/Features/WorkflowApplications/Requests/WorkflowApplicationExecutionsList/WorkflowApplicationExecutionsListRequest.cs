using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationExecutionsList;

public class WorkflowApplicationExecutionsListRequest : IAction<PaginatedResult<WorkflowApplicationExecutionsListResult>>
{
    public Guid WorkflowApplicationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}