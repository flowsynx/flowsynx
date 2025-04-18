namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public enum BackoffStrategy
{
    Fixed,
    Linear,
    Exponential,
    Jitter
}