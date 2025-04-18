namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowConfiguration
{
    public int? DegreeOfParallelism { get; set; } = 3;
    public RetryPolicy? RetryPolicy { get; set; }
    public List<WorkflowTrigger> Triggers { get; set; } = new List<WorkflowTrigger>();
}