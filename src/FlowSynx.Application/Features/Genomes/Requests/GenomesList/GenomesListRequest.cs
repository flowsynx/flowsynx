using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomesList;

public class GenomesListRequest : IAction<PaginatedResult<GenomesListResult>>
{
    public string Namespace { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
