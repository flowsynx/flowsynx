using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeGenesList;

public class ChromosomeGenesListRequest : IAction<PaginatedResult<ChromosomeGenesListResult>>
{
    public Guid ChromosomeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
