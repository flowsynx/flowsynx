namespace FlowSynx.Infrastructure.Workflow;

public interface IPlaceholderReplacer
{
    void ReplacePlaceholdersInParameters(Dictionary<string, object?> parameters, IExpressionParser parser);
}