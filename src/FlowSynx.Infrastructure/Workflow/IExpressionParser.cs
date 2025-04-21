namespace FlowSynx.Infrastructure.Workflow;

public interface IExpressionParser
{
    object? Parse(string? expression);
}