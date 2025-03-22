using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTrigger
{
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}