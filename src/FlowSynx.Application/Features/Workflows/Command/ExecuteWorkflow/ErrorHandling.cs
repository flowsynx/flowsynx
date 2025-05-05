namespace FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

public class ErrorHandling
{
    public ErrorStrategy? Strategy { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}