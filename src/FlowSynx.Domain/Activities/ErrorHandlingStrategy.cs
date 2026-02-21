namespace FlowSynx.Domain.Activities;

public enum ErrorHandlingStrategy
{
    Propagate,
    Ignore,
    Retry,
    Fallback
}