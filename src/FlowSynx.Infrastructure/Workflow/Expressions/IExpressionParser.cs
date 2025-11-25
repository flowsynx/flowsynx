namespace FlowSynx.Infrastructure.Workflow.Expressions;

public interface IExpressionParser
{
    Task<object?> ParseAsync(string? expression, CancellationToken cancellationToken = default);
}