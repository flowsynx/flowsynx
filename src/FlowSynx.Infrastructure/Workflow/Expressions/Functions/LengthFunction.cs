namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns the length of a string or collection
/// </summary>
public class LengthFunction : IFunctionEvaluator
{
    public string Name => "Length";

    public object? Evaluate(List<object?> args)
    {
        if (args.Count != 1)
            throw new ArgumentException("Length() expects exactly 1 argument");

        var arg = args[0];

        if (arg is null)
            return 0d;

        if (arg is string str)
            return (double)str.Length;

        if (arg is System.Collections.ICollection collection)
            return (double)collection.Count;

        if (arg is System.Collections.IEnumerable enumerable)
            return (double)enumerable.Cast<object>().Count();

        throw new ArgumentException($"Length() cannot be applied to type {arg.GetType().Name}");
    }
}