using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Actions.ExecuteWorkflow;

public class ExecuteWorkflowRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid WorkflowId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteWorkflowRequestDefinition
{
    public Dictionary<string, object>? Context { get; set; }
}