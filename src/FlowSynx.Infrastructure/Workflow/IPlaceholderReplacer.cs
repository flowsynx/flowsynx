using FlowSynx.Infrastructure.Workflow.Expressions;

namespace FlowSynx.Infrastructure.Workflow;

public interface IPlaceholderReplacer
{
    Task<string> ReplacePlaceholders(string content, IExpressionParser expressionParser, CancellationToken cancellationToken = default);
    Task ReplacePlaceholdersInParameters(Dictionary<string, object?> parameters, IExpressionParser expressionParser, CancellationToken cancellationToken = default);
}