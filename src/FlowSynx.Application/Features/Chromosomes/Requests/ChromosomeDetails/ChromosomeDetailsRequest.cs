using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeDetails;

public class ChromosomeDetailsRequest : IAction<Result<ChromosomeDetailsResult>>
{
    public Guid Id { get; set; }
}
