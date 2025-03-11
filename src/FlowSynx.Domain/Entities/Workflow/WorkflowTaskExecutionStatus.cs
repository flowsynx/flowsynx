namespace FlowSynx.Domain.Entities.Workflow;

public enum WorkflowTaskExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Retrying
}