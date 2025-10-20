using MediatR;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.AddWorkflowTrigger;

public class AddWorkflowTriggerRequest : AddWorkflowTriggerDefinition, IRequest<Result<AddWorkflowTriggerResponse>>
{
    public required string WorkflowId { get; set; }
}

public class AddWorkflowTriggerDefinition
{
    public WorkflowTriggerStatus Status { get; set; } = WorkflowTriggerStatus.Active;
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public Dictionary<string, object> Properties { get; set; } = new();
}