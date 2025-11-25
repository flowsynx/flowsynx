namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Returns the sum of numeric arguments
/// </summary>
public class SumFunction : NumericFunctionBase
{
    public override string Name => "Sum";

    public override object? Evaluate(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Sum();
    }
}