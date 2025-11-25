namespace FlowSynx.Infrastructure.Workflow.Parsers;

public interface IExpressionParser
{
    object? Parse(string? expression, CancellationToken cancellationToken = default);
}