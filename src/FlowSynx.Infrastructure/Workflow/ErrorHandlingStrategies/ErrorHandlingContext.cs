namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class ErrorHandlingContext
{
    public required string TaskName { get; set; }
    public int RetryCount { get; set; } = 0;
}