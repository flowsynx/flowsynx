namespace FlowSynx.Domain.Workflow;

public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed
}