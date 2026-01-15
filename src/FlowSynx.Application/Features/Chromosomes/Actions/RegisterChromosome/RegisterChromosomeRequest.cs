using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Actions.RegisterChromosome;

public class RegisterChromosomeRequest : IRequest<Result<RegisterChromosomeResult>>
{
    public required object Json { get; set; }
}