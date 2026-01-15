using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Requests.GeneDetails;

public class GeneDetailsRequest : IAction<Result<GeneDetailsResult>>
{
    public Guid Id { get; set; }
}
