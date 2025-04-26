namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class ErrorHandling
{
    public ErrorStrategy? Strategy { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}