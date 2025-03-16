namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public enum WorkflowTaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Retrying
}