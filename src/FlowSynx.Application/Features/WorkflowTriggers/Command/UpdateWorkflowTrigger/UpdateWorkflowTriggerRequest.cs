using MediatR;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.UpdateWorkflowTrigger;

public class UpdateWorkflowTriggerRequest : UpdateWorkflowTriggerDefinition, IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string TriggerId { get; set; }
}

public class UpdateWorkflowTriggerDefinition
{
    public WorkflowTriggerStatus Status { get; set; } = WorkflowTriggerStatus.Active;
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public Dictionary<string, object> Properties { get; set; } = new();
}