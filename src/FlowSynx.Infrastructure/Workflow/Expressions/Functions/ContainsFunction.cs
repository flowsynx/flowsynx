using FlowSynx.Domain;
using FlowSynx.PluginCore.Exceptions;
using System.Collections;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Checks if a container contains a target value
/// </summary>
public class ContainsFunction : IFunctionEvaluator
{
    public string Name => "Contains";

    public object? Evaluate(List<object?> args)
    {
        if (args.Count != 2)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                "Contains() expects exactly 2 arguments");

        var container = args[0];
        var target = args[1];

        if (container is string s)
            return target != null && s.Contains(target.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        if (container is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (string.Equals(item?.ToString(), target?.ToString(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        return string.Equals(container?.ToString(), target?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}