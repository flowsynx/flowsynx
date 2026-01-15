using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomesList;

public class ChromosomesListRequest : IAction<PaginatedResult<ChromosomesListResult>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
