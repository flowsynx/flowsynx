namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public enum ErrorStrategy
{
    Retry,
    Skip,
    Abort,
    TriggerTask
}