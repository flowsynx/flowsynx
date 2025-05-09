namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public enum BackoffStrategy
{
    Fixed,
    Linear,
    Exponential,
    Jitter
}