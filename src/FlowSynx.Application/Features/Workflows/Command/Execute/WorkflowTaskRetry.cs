namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTaskRetry
{
    public int? Max { get; set; }
    public int? Delay { get; set; }
}