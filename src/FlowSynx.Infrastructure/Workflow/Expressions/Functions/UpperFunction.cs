namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Converts string to uppercase
/// </summary>
public class UpperFunction : IFunctionEvaluator
{
    public string Name => "Upper";

    public object? Evaluate(List<object?> args)
    {
        if (args.Count != 1)
            throw new ArgumentException("Upper() expects exactly 1 argument");

        return args[0]?.ToString()?.ToUpperInvariant();
    }
}