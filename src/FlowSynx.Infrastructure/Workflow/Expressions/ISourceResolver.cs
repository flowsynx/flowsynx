namespace FlowSynx.Infrastructure.Workflow.Expressions;

public interface ISourceResolver
{
    Task<object?> ResolveAsync(string key, CancellationToken cancellationToken = default);
}