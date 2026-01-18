using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeChromosomeList;

public class GenomeChromosomeListRequest : IAction<PaginatedResult<GenomeChromosomeListResult>>
{
    public Guid GenomeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
