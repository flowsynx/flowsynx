using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Actions.ExecuteGenome;

public class ExecuteGenomeRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid GenomeId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteGenomeRequestDefinition
{
    public Dictionary<string, object>? Context { get; set; }
}