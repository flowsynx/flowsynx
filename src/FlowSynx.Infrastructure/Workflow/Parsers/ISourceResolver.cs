namespace FlowSynx.Infrastructure.Workflow.Parsers;

public interface ISourceResolver
{
    Task<object?> Resolve(string key, CancellationToken cancellationToken = default);
}