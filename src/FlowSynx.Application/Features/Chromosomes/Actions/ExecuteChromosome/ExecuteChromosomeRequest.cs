using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Chromosomes.Actions.ExecuteChromosome;

public class ExecuteChromosomeRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid ChromosomeId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteChromosomeRequestDefinition
{
    public Dictionary<string, object>? Context { get; set; }
}