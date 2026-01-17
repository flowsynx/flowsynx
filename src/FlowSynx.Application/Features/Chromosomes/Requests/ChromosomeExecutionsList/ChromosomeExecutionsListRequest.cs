using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeExecutionsList;

public class ChromosomeExecutionsListRequest : IAction<PaginatedResult<ChromosomeExecutionsListResult>>
{
    public Guid ChromosomeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
