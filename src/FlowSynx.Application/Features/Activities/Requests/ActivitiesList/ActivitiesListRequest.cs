using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Requests.ActivitiesList;

public class ActivitiesListRequest : IAction<PaginatedResult<ActivitiesListResult>>
{
    public string? Namespace { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
