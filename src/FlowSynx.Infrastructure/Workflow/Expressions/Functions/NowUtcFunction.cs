using System;
using System.Globalization;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns the current UTC date/time as an ISO 8601 string.
/// Usage: NowUtc()
/// </summary>
public class NowUtcFunction : IFunctionEvaluator
{
    public string Name => "NowUtc";

    public object? Evaluate(List<object?> args)
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }
}