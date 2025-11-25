namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Converts string to lowercase
/// </summary>
public class LowerFunction : IFunctionEvaluator
{
    public string Name => "Lower";

    public object? Evaluate(List<object?> args)
    {
        if (args.Count != 1)
            throw new ArgumentException("Lower() expects exactly 1 argument");

        return args[0]?.ToString()?.ToLowerInvariant();
    }
}