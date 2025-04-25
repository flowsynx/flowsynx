namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTaskResult
{
    public bool Succeeded { get; set; }
    public object? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    public static WorkflowTaskResult Success(object? output = null) =>
        new() { Succeeded = true, Output = output };

    public static WorkflowTaskResult Fail(string errorMessage, Exception? exception = null) =>
        new() { Succeeded = false, ErrorMessage = errorMessage, Exception = exception };
}
