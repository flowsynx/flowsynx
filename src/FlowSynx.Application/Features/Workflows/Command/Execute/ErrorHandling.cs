namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class ErrorHandling
{
    public ErrorStrategy? Strategy { get; set; } = ErrorStrategy.Abort;
    public RetryPolicy? RetryPolicy { get; set; }
}