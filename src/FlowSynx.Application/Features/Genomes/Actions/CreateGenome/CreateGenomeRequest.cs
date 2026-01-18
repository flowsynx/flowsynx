using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Actions.CreateGenome;

public class CreateGenomeRequest : IRequest<Result<CreateGenomeResult>>
{
    public required object Json { get; set; }
}