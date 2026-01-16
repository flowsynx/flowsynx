using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Requests.GenesList;

public class GenesListRequest : IAction<PaginatedResult<GenesListResult>>
{
    public string? Namespace { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
