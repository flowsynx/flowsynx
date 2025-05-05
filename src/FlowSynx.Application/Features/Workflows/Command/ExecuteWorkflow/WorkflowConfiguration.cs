namespace FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

public class WorkflowConfiguration
{
    public int? DegreeOfParallelism { get; set; } = 3;
    public ErrorHandling? ErrorHandling { get; set; }
    public int? Timeout { get; set; }
    public List<WorkflowTrigger> Triggers { get; set; } = new();
}