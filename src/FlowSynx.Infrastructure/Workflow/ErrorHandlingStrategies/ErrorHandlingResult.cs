namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class ErrorHandlingResult
{
    public bool ShouldRetry { get; set; }
    public bool ShouldSkip { get; set; }
    public bool ShouldAbortWorkflow { get; set; }
    public bool ShouldTriggerTask { get; set; }
    public string? TaskToTrigger { get; set; }
}