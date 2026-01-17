using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosome.Actions.CreateChromosome;

public class CreateChromosomeRequest : IRequest<Result<CreateChromosomeResult>>
{
    public required object Json { get; set; }
}