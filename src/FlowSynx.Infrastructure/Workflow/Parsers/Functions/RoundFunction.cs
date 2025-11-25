namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Rounds a numeric value to specified decimal places
/// Usage: Round(value) or Round(value, decimals)
/// </summary>
public class RoundFunction : NumericFunctionBase
{
    public override string Name => "Round";

    public override object? Evaluate(List<object?> args)
    {
        if (args.Count == 0 || args.Count > 2)
            throw new ArgumentException("Round() expects 1 or 2 arguments");

        var nums = ExtractNumericValues(new[] { args[0] }).ToList();
        if (nums.Count == 0) return 0d;

        var value = nums[0];
        var decimals = 0;

        if (args.Count == 2)
        {
            var decimalsList = ExtractNumericValues(new[] { args[1] }).ToList();
            if (decimalsList.Count > 0)
                decimals = (int)decimalsList[0];
        }

        return Math.Round(value, decimals);
    }
}