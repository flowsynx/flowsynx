namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ErrorHandling
{
    public ErrorStrategy? Strategy { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}