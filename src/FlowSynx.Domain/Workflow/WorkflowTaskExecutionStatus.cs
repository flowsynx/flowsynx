namespace FlowSynx.Domain.Workflow;

public enum WorkflowTaskExecutionStatus
{
    Pending,
    Running,
    Completed,
    Canceled,
    Failed,
    Retrying
}