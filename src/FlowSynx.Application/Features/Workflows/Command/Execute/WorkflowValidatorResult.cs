namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowValidatorResult
{
    public bool Cyclic { get; set; }
    public List<string> CyclicNodes { get; set; } = new List<string>();
}