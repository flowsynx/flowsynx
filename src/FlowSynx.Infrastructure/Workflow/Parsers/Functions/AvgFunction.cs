namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Returns the average of numeric arguments
/// </summary>
public class AvgFunction : NumericFunctionBase
{
    public override string Name => "Avg";

    public override object? Evaluate(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Average();
    }
}