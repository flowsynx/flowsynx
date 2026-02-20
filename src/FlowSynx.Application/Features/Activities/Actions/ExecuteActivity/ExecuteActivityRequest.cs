using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Actions.ExecuteActivity;

public class ExecuteActivityRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid ActivityId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteActivityRequestDefinition
{
    public Dictionary<string, object>? Params { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}