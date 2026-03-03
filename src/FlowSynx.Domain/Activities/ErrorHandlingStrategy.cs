namespace FlowSynx.Domain.Activities;

public enum ErrorHandlingStrategy
{
    Propagate,
    Continue,
    Retry,
    Fallback
}