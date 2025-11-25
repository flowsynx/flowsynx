using System;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns a new GUID or normalizes a provided GUID string.
/// Usage:
/// - Guid() -> new GUID string
/// - Guid("existing-guid-string") -> canonical GUID string if parseable, otherwise a new GUID string
/// </summary>
public class GuidFunction : IFunctionEvaluator
{
    public string Name => "Guid";

    public object? Evaluate(List<object?> args)
    {
        if (args == null || args.Count == 0)
            return System.Guid.NewGuid().ToString();

        var s = args[0]?.ToString();
        if (string.IsNullOrWhiteSpace(s))
            return System.Guid.NewGuid().ToString();

        if (System.Guid.TryParse(s, out var parsed))
            return parsed.ToString();

        return System.Guid.NewGuid().ToString();
    }
}