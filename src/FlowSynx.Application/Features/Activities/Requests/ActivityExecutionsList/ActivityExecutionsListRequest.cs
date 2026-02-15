using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Requests.ActivityExecutionsList;

public class ActivityExecutionsListRequest : IAction<PaginatedResult<ActivityExecutionsListResult>>
{
    public Guid ActivityId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
