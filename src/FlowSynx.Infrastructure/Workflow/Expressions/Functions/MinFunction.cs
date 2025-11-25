namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns the minimum value from numeric arguments
/// </summary>
public class MinFunction : NumericFunctionBase
{
    public override string Name => "Min";

    public override object? Evaluate(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Min();
    }
}