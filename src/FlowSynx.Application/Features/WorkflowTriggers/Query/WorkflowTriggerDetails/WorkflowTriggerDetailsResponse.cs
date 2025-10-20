using FlowSynx.Domain.Trigger;

namespace FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggerDetails;

public class WorkflowTriggerDetailsResponse
{
    public Guid Id { get; set; }
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public WorkflowTriggerStatus Status { get; set; } = WorkflowTriggerStatus.Active;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public DateTime? LastModified { get; set; }
}