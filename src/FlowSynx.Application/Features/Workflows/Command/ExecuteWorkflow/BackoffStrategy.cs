namespace FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

public enum BackoffStrategy
{
    Fixed,
    Linear,
    Exponential,
    Jitter
}