using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.Workflow;

public interface IPlaceholderReplacer
{
    string ReplacePlaceholders(string content, IExpressionParser parser);
    void ReplacePlaceholdersInParameters(Dictionary<string, object?> parameters, IExpressionParser parser);
}