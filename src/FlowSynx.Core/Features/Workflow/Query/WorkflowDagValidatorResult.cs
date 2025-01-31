namespace FlowSynx.Core.Features.Workflow.Query;

public class WorkflowDagValidatorResult
{
    public bool Cyclic { get; set; }
    public List<string> CyclicNodes { get; set; } = new List<string>();
}