using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.GeneBlueprints.Requests.GeneblueprintsList;

public class GeneblueprintsListRequest : IAction<PaginatedResult<GeneblueprintsListResult>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
