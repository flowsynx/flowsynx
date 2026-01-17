using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Actions.ValidateChromosome;

public class ValidateChromosomeRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}