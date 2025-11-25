namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Returns the maximum value from numeric arguments
/// </summary>
public class MaxFunction : NumericFunctionBase
{
    public override string Name => "Max";

    public override object? Evaluate(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Max();
    }
}