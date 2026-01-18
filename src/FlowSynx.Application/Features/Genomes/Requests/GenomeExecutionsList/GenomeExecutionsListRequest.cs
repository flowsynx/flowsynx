using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeExecutionsList;

public class GenomeExecutionsListRequest : IAction<PaginatedResult<GenomeExecutionsListResult>>
{
    public Guid GenomeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}