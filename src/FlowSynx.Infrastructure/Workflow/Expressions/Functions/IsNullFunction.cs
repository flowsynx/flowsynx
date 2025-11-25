using System.Collections;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Workflow.Expressions.Functions;

/// <summary>
/// Returns true when the argument is null/empty:
/// - null or JToken null -> true
/// - empty string -> true
/// - empty enumerable (non-string) -> true
/// Otherwise false.
/// Usage: IsNull(value)
/// </summary>
public class IsNullFunction : IFunctionEvaluator
{
    public string Name => "IsNull";

    public object? Evaluate(List<object?> args)
    {
        if (args == null || args.Count == 0)
            return true;

        var val = args[0];

        if (val == null)
            return true;

        // Unwrap JToken
        if (val is JToken jt)
        {
            if (jt.Type == JTokenType.Null) return true;
            if (jt.Type == JTokenType.String && string.IsNullOrEmpty(jt.ToString())) return true;
        }

        if (val is string s)
            return string.IsNullOrEmpty(s);

        // Check empty enumerable (but not string)
        if (val is IEnumerable enumerable && val is not string)
        {
            var enumerator = enumerable.GetEnumerator();
            try
            {
                if (!enumerator.MoveNext())
                    return true;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        return false;
    }
}