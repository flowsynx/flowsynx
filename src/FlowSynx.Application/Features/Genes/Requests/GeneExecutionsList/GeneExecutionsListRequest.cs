using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Features.Genes.Requests.GeneExecutionsList;

public class GeneExecutionsListRequest : IAction<PaginatedResult<GeneExecutionsListResult>>
{
    public Guid GeneId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
