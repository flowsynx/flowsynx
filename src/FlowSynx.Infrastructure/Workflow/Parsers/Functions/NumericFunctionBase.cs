using System.Collections;
using System.Globalization;

namespace FlowSynx.Infrastructure.Workflow.Parsers.Functions;

/// <summary>
/// Base class for functions that operate on numeric values
/// </summary>
public abstract class NumericFunctionBase : IFunctionEvaluator
{
    public abstract string Name { get; }

    public abstract object? Evaluate(List<object?> args);

    protected static IEnumerable<double> ExtractNumericValues(IEnumerable<object?> values)
    {
        foreach (var v in values)
        {
            if (v == null) continue;

            if (v is IEnumerable enumerable and not string)
            {
                foreach (var inner in enumerable)
                {
                    if (inner == null) continue;
                    if (double.TryParse(inner.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dn))
                        yield return dn;
                }
                continue;
            }

            if (double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                yield return d;
        }
    }
}