namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowValidatorResult
{
    public bool Cyclic { get; set; }
    public List<string> CyclicNodes { get; set; } = new List<string>();
}