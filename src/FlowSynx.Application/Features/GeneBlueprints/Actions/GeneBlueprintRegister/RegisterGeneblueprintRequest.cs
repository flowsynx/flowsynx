using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.GeneBlueprints.Actions.GeneBlueprintRegister;

public class RegisterGeneblueprintRequest : IRequest<Result<RegisterGeneblueprintResult>>
{
    public required object Json { get; set; }
}