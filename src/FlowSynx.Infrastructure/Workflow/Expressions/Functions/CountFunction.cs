using System.Collections;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns the count of arguments or items in a collection
/// </summary>
public class CountFunction : IFunctionEvaluator
{
    public string Name => "Count";

    public object? Evaluate(List<object?> args)
    {
        if (args.Count == 1 && args[0] is IEnumerable enumerable and not string)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return count;
        }
        return args.Count;
    }
}