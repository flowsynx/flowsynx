namespace FlowSynx.Infrastructure.Workflow;

public sealed record class TaskOutput(object? Result, TaskOutputStatus Status)
{
    public static TaskOutput Success(object? result) => new(result, TaskOutputStatus.Succeeded);
    public static TaskOutput Failure(object? error = null) => new(error, TaskOutputStatus.Failed);
}

public enum TaskOutputStatus
{
    Succeeded,
    Failed
}