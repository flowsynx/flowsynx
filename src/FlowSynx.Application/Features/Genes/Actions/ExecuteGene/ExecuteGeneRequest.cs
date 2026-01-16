using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Actions.ExecuteGene;

public class ExecuteGeneRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid GeneId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteGeneRequestDefinition
{
    public Dictionary<string, object>? Parameters { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}