namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTaskExecutionResult
{
    public object? Result { get; set; }
    public WorkflowTaskStatus Status { get; set; }
}