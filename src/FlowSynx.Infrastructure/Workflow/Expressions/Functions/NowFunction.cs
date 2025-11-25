using System;
using System.Globalization;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns the current local date/time as an ISO 8601 string (invariant culture).
/// Usage: Now()
/// </summary>
public class NowFunction : IFunctionEvaluator
{
    public string Name => "Now";

    public object? Evaluate(List<object?> args)
    {
        return DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
    }
}