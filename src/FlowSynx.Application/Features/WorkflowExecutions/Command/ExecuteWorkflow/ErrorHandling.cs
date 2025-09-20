namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ErrorHandling
{
    public ErrorStrategy? Strategy { get; set; } = ErrorStrategy.Abort;
    public RetryPolicy? RetryPolicy { get; set; } = new RetryPolicy();
}