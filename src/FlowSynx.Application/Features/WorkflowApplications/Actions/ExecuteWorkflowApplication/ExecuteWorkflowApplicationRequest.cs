using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.ExecuteWorkflowApplication;

public class ExecuteWorkflowApplicationRequest : IRequest<Result<ExecutionResponse>>
{
    public required Guid WorkflowApplicationId { get; set; }
    public required object Json { get; set; }
}

public class ExecuteWorkflowApplicationRequestDefinition
{
    public Dictionary<string, object>? Context { get; set; }
}