using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Actions.ValidateGenome;

public class ValidateGenomeRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}