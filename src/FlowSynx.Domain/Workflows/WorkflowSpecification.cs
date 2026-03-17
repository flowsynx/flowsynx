using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.Workflows;

public class WorkflowSpecification
{
    public string Description { get; set; } = string.Empty;
    public List<ActivityInstance> Activities { get; set; } = new List<ActivityInstance>();
    public FaultHandling FaultHandling { get; set; } = new();
    //public ResourceConstraints Resources { get; set; } = new();
    public CompatibilityConstraints? Constraints { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public SecurityContext Security { get; set; } = new();
    public WorkflowValidation Validation { get; set; } = new();
    public WorkflowOutput Output { get; set; } = new();
}