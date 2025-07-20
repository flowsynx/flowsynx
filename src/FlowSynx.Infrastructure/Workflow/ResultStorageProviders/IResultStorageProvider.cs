namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public interface IResultStorageProvider
{
    string Type { get; }

    Task<string> SaveResultAsync(
        WorkflowExecutionContext executionContext,
        Guid workflowTaskId,
        object result,
        CancellationToken cancellationToken = default);

    Task<T?> LoadResultAsync<T>(
        WorkflowExecutionContext executionContext,
        Guid workflowTaskId,
        CancellationToken cancellationToken = default);
}