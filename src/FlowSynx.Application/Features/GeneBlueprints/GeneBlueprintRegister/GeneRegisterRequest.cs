using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.GeneBlueprints.GeneBlueprintRegister;

public class GeneRegisterRequest : IRequest<Result<GeneRegisterResult>>
{
    public required object Json { get; set; }
}