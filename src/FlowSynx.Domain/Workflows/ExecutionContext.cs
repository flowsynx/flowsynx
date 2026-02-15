using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.Workflows;

public class ExecutionContext
{
    public FaultHandling FaultHandling { get; set; } = new();
    public ResourceConstraints Resources { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public Dictionary<string, object> SharedState { get; set; } = new();
    public SecurityContext Security { get; set; } = new();
}