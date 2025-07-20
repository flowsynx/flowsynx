using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public interface IResultStorageProvider
{
    string Name { get; }

    Task<string> SaveResultAsync(
        WorkflowExecutionContext executionContext,
        ConcurrentDictionary<string, object?> results,
        CancellationToken cancellationToken = default);

    Task<ConcurrentDictionary<string, object?>?> LoadResultAsync(
        WorkflowExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}