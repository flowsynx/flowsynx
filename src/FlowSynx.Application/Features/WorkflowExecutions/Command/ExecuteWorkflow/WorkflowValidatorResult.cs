namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowValidatorResult
{
    public bool Cyclic { get; set; }
    public List<string> CyclicNodes { get; set; } = new();
}