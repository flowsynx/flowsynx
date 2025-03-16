using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTrigger
{
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public string? Details { get; set; }
}