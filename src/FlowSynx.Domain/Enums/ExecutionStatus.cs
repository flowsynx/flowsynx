namespace FlowSynx.Domain.Enums;

public enum ExecutionStatus
{
    Idle,
    Running,
    Completed,
    Skipped,
    Faulted,
    Terminated
}