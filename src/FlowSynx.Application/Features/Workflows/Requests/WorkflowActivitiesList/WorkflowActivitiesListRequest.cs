using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowActivitiesList;

public class WorkflowActivitiesListRequest : IAction<PaginatedResult<WorkflowActivitiesListResult>>
{
    public Guid WorkflowId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
