using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

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
            foreach (var d in ExtractNumbers(v))
                yield return d;
        }
    }

    private static IEnumerable<double> ExtractNumbers(object? value)
    {
        if (value == null)
            yield break;

        // Handle JToken specially
        if (value is JToken jt)
        {
            foreach (var d in ExtractFromJToken(jt))
                yield return d;
            yield break;
        }

        // Handle nested enumerable but not string
        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var inner in enumerable)
            {
                foreach (var d in ExtractNumbers(inner))
                    yield return d;
            }
            yield break;
        }

        // Attempt to parse primitive value
        if (TryParseDouble(value, out var result))
            yield return result;
    }

    private static IEnumerable<double> ExtractFromJToken(JToken jt)
    {
        if (jt.Type == JTokenType.Array || jt.Type == JTokenType.Object)
        {
            foreach (var inner in jt)
            {
                foreach (var d in ExtractNumbers(inner))
                    yield return d;
            }
        }
        else
        {
            var jvString = jt.Type == JTokenType.Null ? null : jt.ToString();
            if (jvString != null && double.TryParse(jvString, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                yield return result;
        }
    }

    private static bool TryParseDouble(object value, out double result)
    {
        return double.TryParse(
            value?.ToString(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result
        );
    }
}