namespace FlowSynx.Domain.Workflow;

public enum WorkflowTaskExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Retrying
}