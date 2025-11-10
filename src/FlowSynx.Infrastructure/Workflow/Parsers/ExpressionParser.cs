using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Globalization;

namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParser : IExpressionParser
{
    private readonly Dictionary<string, object?> _outputs;
    private readonly Dictionary<string, object?> _variables;
    private const string OutputsPrefix = "Outputs(";
    private const string VariablesPrefix = "Variables(";

    // Supported functional methods
    private static readonly HashSet<string> _functionalMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Min", "Max", "Sum", "Avg", "Count", "Contains"
    };

    public ExpressionParser(Dictionary<string, object?> outputs, Dictionary<string, object?> variables)
    {
        _outputs = outputs;
        _variables = variables;
    }

    public object? Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        return ResolveExpression(expression);
    }

    private object? ResolveExpression(string expr)
    {
        int i = 0;
        while (i < expr.Length)
        {
            if (i + 2 < expr.Length && expr[i] == '$' && expr[i + 1] == '[')
            {
                int end = FindMatchingBracket(expr, i + 1);
                if (end == -1)
                    throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                        $"Unbalanced brackets in expression: {expr}");

                string inner = expr.Substring(i + 2, end - i - 2).Trim();
                object? resolved = ResolveInnerOrConditionalOrMath(inner);

                if (expr.Trim() == expr.Substring(i, end - i + 1))
                    return resolved;

                expr = expr.Substring(0, i) + (resolved?.ToString() ?? string.Empty) + expr.Substring(end + 1);
                i += (resolved?.ToString() ?? string.Empty).Length;
            }
            else i++;
        }
        return expr;
    }

    private object? ResolveInnerOrConditionalOrMath(string inner)
    {
        if (inner.Contains('?') && inner.Contains(':'))
            return EvaluateConditionalExpression(inner);

        // functional methods (multi-arg) detection
        if (TryEvaluateFunctionalExpression(inner, out var fnResult))
            return fnResult;

        if (ContainsOperator(inner))
            return EvaluateBooleanExpression(inner);

        if (inner.IndexOfAny(new[] { '+', '-', '*', '/', '%' }) >= 0)
            return EvaluateArithmeticExpression(inner);

        return ResolveInnerExpression(inner);
    }

    private bool TryEvaluateFunctionalExpression(string inner, out object? result)
    {
        result = null;
        inner = inner.Trim();
        int parenIdx = inner.IndexOf('(');
        if (parenIdx <= 0) return false;

        string name = inner.Substring(0, parenIdx).Trim();
        if (!_functionalMethods.Contains(name)) return false;

        int endParen = FindMatchingParenthesis(inner, parenIdx);
        if (endParen == -1)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"Unbalanced parentheses in functional expression: {inner}");

        string argsSegment = inner.Substring(parenIdx + 1, endParen - parenIdx - 1);
        var args = SplitArguments(argsSegment);

        var evaluatedArgs = args.Select(EvaluateFunctionalArgument).ToList();

        result = name.ToLowerInvariant() switch
        {
            "min" => EvaluateMin(evaluatedArgs),
            "max" => EvaluateMax(evaluatedArgs),
            "sum" => EvaluateSum(evaluatedArgs),
            "avg" => EvaluateAvg(evaluatedArgs),
            "count" => EvaluateCount(evaluatedArgs),
            "contains" => EvaluateContains(evaluatedArgs),
            _ => null
        };
        return true;
    }

    private static List<string> SplitArguments(string argsSegment)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(argsSegment)) return list;

        int depth = 0;
        bool inQuotes = false;
        char quoteChar = '\0';
        int start = 0;

        for (int i = 0; i < argsSegment.Length; i++)
        {
            char c = argsSegment[i];

            if ((c == '\'' || c == '"'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (quoteChar == c)
                {
                    inQuotes = false;
                }
            }

            if (!inQuotes)
            {
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    list.Add(argsSegment.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
        }
        // last arg
        if (start < argsSegment.Length)
            list.Add(argsSegment.Substring(start).Trim());

        return list.Where(a => a.Length > 0).ToList();
    }

    private object? EvaluateFunctionalArgument(string arg)
    {
        arg = arg.Trim();
        // If argument itself is another functional expression
        if (TryEvaluateFunctionalExpression(arg, out var fnValue))
            return fnValue;

        // Wrap into $[...] to reuse existing parsing for complex constructs
        if (arg.StartsWith(OutputsPrefix) || arg.StartsWith(VariablesPrefix) ||
            ContainsOperator(arg) || arg.Contains("$[") || arg.Contains('?') || arg.Contains(':'))
        {
            return Parse($"$[{arg}]");
        }

        // Literal or simple value
        var lit = ResolveLiteralOrValue(arg);
        return lit;
    }

    private static IEnumerable<double> ExtractNumericValues(IEnumerable<object?> values)
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

    private static object EvaluateMin(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Min();
    }

    private static object EvaluateMax(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Max();
    }

    private static object EvaluateSum(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Sum();
    }

    private static object EvaluateAvg(List<object?> args)
    {
        var nums = ExtractNumericValues(args).ToList();
        if (nums.Count == 0) return 0d;
        return nums.Average();
    }

    private static object EvaluateCount(List<object?> args)
    {
        if (args.Count == 1 && args[0] is IEnumerable enumerable and not string)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return count;
        }
        return args.Count;
    }

    private static object EvaluateContains(List<object?> args)
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

        // Fallback single value comparison
        return string.Equals(container?.ToString(), target?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private object? EvaluateConditionalExpression(string expr)
    {
        int questionIdx = -1, colonIdx = -1, depth = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            if (expr[i] == '?')
            {
                if (depth == 0 && questionIdx == -1)
                    questionIdx = i;
                depth++;
            }
            else if (expr[i] == ':')
            {
                depth--;
                if (colonIdx == -1 && depth == 0)
                    colonIdx = i;
            }
        }

        if (questionIdx == -1 || colonIdx == -1 || colonIdx < questionIdx)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid conditional expression: {expr}");

        string condition = expr[..questionIdx].Trim();
        string truePart = expr[(questionIdx + 1)..colonIdx].Trim();
        string falsePart = expr[(colonIdx + 1)..].Trim();

        bool conditionResult = EvaluateBooleanExpression(condition);
        return Parse($"$[{(conditionResult ? truePart : falsePart)}]");
    }

    private bool EvaluateBooleanExpression(string expr)
    {
        expr = expr.Trim();
        expr = ReplaceEmbeddedExpressions(expr);

        // handle NOT
        if (expr.StartsWith('!'))
            return !EvaluateBooleanExpression(expr[1..]);

        // parentheses
        if (expr.StartsWith('(') && expr.EndsWith(')'))
            return EvaluateBooleanExpression(expr[1..^1]);

        // AND / OR
        if (expr.Contains("&&"))
        {
            var parts = expr.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            return parts.All(p => EvaluateBooleanExpression(p));
        }
        if (expr.Contains("||"))
        {
            var parts = expr.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Any(p => EvaluateBooleanExpression(p));
        }

        // comparison
        string[] ops = { ">=", "<=", "==", "!=", ">", "<" };
        foreach (var op in ops)
        {
            int idx = expr.IndexOf(op, StringComparison.Ordinal);
            if (idx > 0)
            {
                string left = expr[..idx].Trim();
                string right = expr[(idx + op.Length)..].Trim();

                object? lVal = EvaluateArithmeticExpression(left);
                object? rVal = EvaluateArithmeticExpression(right);

                return Compare(lVal, rVal, op);
            }
        }

        // fallback bool literal
        if (bool.TryParse(expr, out bool boolVal)) return boolVal;

        throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid boolean expression: {expr}");
    }

    private object? EvaluateArithmeticExpression(string expr)
    {
        expr = ReplaceEmbeddedExpressions(expr);
        expr = ReplaceVariables(expr);

        try
        {
            using var dt = new DataTable();
            var value = dt.Compute(expr, null);
            return Convert.ToDouble(value);
        }
        catch
        {
            return ResolveLiteralOrValue(expr);
        }
    }

    private string ReplaceVariables(string expr)
    {
        int pos = 0;
        while (pos < expr.Length)
        {
            if (expr.Substring(pos).StartsWith(OutputsPrefix) || expr.Substring(pos).StartsWith(VariablesPrefix))
            {
                bool isOutput = expr.Substring(pos).StartsWith(OutputsPrefix);
                string sourceType = isOutput ? "Outputs" : "Variables";
                int start = pos + sourceType.Length; // position of '('
                int end = FindMatchingParenthesis(expr, start);
                if (end == -1)
                {
                    pos++;
                    continue;
                }

                // Resolve root key (may itself be an expression like Variables('DynamicKey'))
                string keyExpr = expr.Substring(start + 1, end - start - 1);
                string resolvedKey = StripQuotes(ResolveTopLevelExpression(keyExpr));
                object? rootValue = ResolveSourceValue(sourceType, resolvedKey);

                // Collect optional access path (e.g. .Scores[0].Items[2])
                int pathStart = end + 1;
                int scan = pathStart;
                while (scan < expr.Length)
                {
                    char c = expr[scan];
                    if (c == '.')
                    {
                        scan++; // consume '.'
                        // read property name
                        while (scan < expr.Length)
                        {
                            char pc = expr[scan];
                            if (char.IsLetterOrDigit(pc) || pc == '_')
                                scan++;
                            else
                                break;
                        }
                    }
                    else if (c == '[')
                    {
                        scan++; // after '['
                        // consume until matching ']'
                        while (scan < expr.Length && expr[scan] != ']')
                            scan++;
                        if (scan < expr.Length && expr[scan] == ']')
                            scan++; // consume ']'
                        else
                            break; // malformed -> stop
                    }
                    else
                    {
                        break; // end of access path
                    }
                }

                string accessPath = scan > pathStart ? expr.Substring(pathStart, scan - pathStart) : string.Empty;
                object? finalValue = string.IsNullOrEmpty(accessPath) ? rootValue : GetNestedValue(rootValue, accessPath);

                string replacement = finalValue?.ToString() ?? "0";
                expr = expr.Substring(0, pos) + replacement + expr.Substring(scan);
                pos += replacement.Length;
            }
            else
            {
                pos++;
            }
        }
        return expr;
    }

    private string ReplaceEmbeddedExpressions(string expr)
    {
        int pos = 0;
        while (pos < expr.Length)
        {
            int start = expr.IndexOf("$[", pos, StringComparison.Ordinal);
            if (start == -1) break;

            int end = FindMatchingBracket(expr, start + 1);
            if (end == -1)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                    $"Unbalanced brackets in expression: {expr}");

            string inner = expr.Substring(start + 2, end - start - 2).Trim();
            object? val = ResolveInnerOrConditionalOrMath(inner);
            expr = expr.Substring(0, start) + (val?.ToString() ?? "null") + expr.Substring(end + 1);
            pos = start + (val?.ToString() ?? "null").Length;
        }
        return expr;
    }

    private static bool Compare(object? left, object? right, string op)
    {
        double? lNum = TryConvertToDouble(left);
        double? rNum = TryConvertToDouble(right);

        return op switch
        {
            "==" => string.Equals(left?.ToString(), right?.ToString(), StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(left?.ToString(), right?.ToString(), StringComparison.OrdinalIgnoreCase),
            ">" => lNum.HasValue && rNum.HasValue && lNum > rNum,
            "<" => lNum.HasValue && rNum.HasValue && lNum < rNum,
            ">=" => lNum.HasValue && rNum.HasValue && lNum >= rNum,
            "<=" => lNum.HasValue && rNum.HasValue && lNum <= rNum,
            _ => false
        };
    }

    private static double? TryConvertToDouble(object? val)
    {
        if (val == null) return null;
        if (double.TryParse(val.ToString(), out double d)) return d;
        return null;
    }

    private object? ResolveLiteralOrValue(string str)
    {
        str = str.Trim();

        if (str.StartsWith('\'') && str.EndsWith('\'')) return StripQuotes(str);

        if (str.StartsWith(OutputsPrefix) || str.StartsWith(VariablesPrefix))
            return ResolveInnerExpression(str);

        if (double.TryParse(str, out double num)) return num;
        if (bool.TryParse(str, out bool b)) return b;

        return str;
    }

    private object? EvaluateExpression(string inner)
    {
        if (inner.Contains('?') && inner.Contains(':'))
        {
            var parts = SplitTernary(inner);
            if (parts == null)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid expression: {inner}");

            var condition = parts.Value.condition.Trim();
            var trueExpr = parts.Value.ifTrue.Trim();
            var falseExpr = parts.Value.ifFalse.Trim();

            var conditionResult = EvaluateBooleanExpression(condition);
            return conditionResult
                ? Parse($"$[{trueExpr}]")
                : Parse($"$[{falseExpr}]");
        }

        return EvaluateBooleanExpression(inner);
    }

    private static (string condition, string ifTrue, string ifFalse)? SplitTernary(string expr)
    {
        int qIndex = -1;
        int colonIndex = -1;
        int depth = 0;
        bool inQuotes = false;

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '\'' || c == '"')
                inQuotes = !inQuotes;
            if (inQuotes) continue;

            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '?' && depth == 0)
                qIndex = i;
            else if (c == ':' && depth == 0 && qIndex != -1)
            {
                colonIndex = i;
                break;
            }
        }

        if (qIndex == -1 || colonIndex == -1)
            return null;

        string condition = expr.Substring(0, qIndex);
        string ifTrue = expr.Substring(qIndex + 1, colonIndex - qIndex - 1);
        string ifFalse = expr.Substring(colonIndex + 1);

        return (condition, ifTrue, ifFalse);
    }

    private static bool ContainsOperator(string inner)
    {
        return inner.Contains("==") || inner.Contains("!=") ||
               inner.Contains(">=") || inner.Contains("<=") ||
               inner.Contains(">") || inner.Contains("<") ||
               inner.Contains("&&") || inner.Contains("||") ||
               inner.Contains("?") || inner.Contains(":");
    }

    private object? ResolveInnerExpression(string inner)
    {
        inner = inner.Trim();

        if (TryEvaluateFunctionalExpression(inner, out var fnValue))
            return fnValue;

        if (ContainsOperator(inner))
            return EvaluateExpression(inner);

        if (IsLiteral(inner))
            return ParseLiteral(inner);

        string sourceType;
        if (inner.StartsWith(OutputsPrefix))
            sourceType = "Outputs";
        else if (inner.StartsWith(VariablesPrefix))
            sourceType = "Variables";
        else
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid expression: {inner}");

        int startKey = sourceType.Length + 1;
        int endKey = FindMatchingParenthesis(inner, startKey - 1);
        if (endKey == -1)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Unbalanced parentheses: {inner}");

        string keyExpr = inner.Substring(startKey, endKey - startKey).Trim();
        string accessPath = inner.Substring(endKey + 1).Trim();

        string resolvedKey = StripQuotes(ResolveTopLevelExpression(keyExpr));
        object? value = ResolveSourceValue(sourceType, resolvedKey);

        if (!string.IsNullOrEmpty(accessPath))
            value = GetNestedValue(value, accessPath);

        return value;
    }

    private static bool IsLiteral(string inner)
    {
        if (string.IsNullOrWhiteSpace(inner))
            return false;

        // Quoted string, number, boolean, or null
        return
            (inner.StartsWith('\'') && inner.EndsWith('\'')) ||
            (inner.StartsWith('\"') && inner.EndsWith('\"')) ||
            double.TryParse(inner, out _) ||
            bool.TryParse(inner, out _) ||
            inner.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    private static object? ParseLiteral(string inner)
    {
        inner = inner.Trim();

        if ((inner.StartsWith('\'') && inner.EndsWith('\'')) ||
            (inner.StartsWith('\"') && inner.EndsWith('\"')))
            return inner.Substring(1, inner.Length - 2);

        if (bool.TryParse(inner, out bool b))
            return b;

        if (double.TryParse(inner, out double d))
            return d;

        if (inner.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        return inner;
    }

    private string ResolveTopLevelExpression(string expr)
    {
        expr = expr.Trim();
        if (expr.StartsWith(OutputsPrefix) || expr.StartsWith(VariablesPrefix))
            return Parse($"$[{expr}]")?.ToString() ?? string.Empty;

        return expr;
    }

    private object? ResolveSourceValue(string sourceType, string key)
    {
        var dict = sourceType.Equals("Outputs", StringComparison.OrdinalIgnoreCase) ? _outputs : _variables;

        if (!dict.TryGetValue(key, out var value))
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"ExpressionParser: {sourceType}('{key}') not found");

        return value;
    }

    private static string StripQuotes(string str)
    {
        if (str.Length >= 2 && str[0] == '\'' && str[^1] == '\'')
            return str.Substring(1, str.Length - 2);
        return str;
    }

    private static int FindMatchingBracket(string expr, int start)
    {
        int depth = 0;
        for (int i = start; i < expr.Length; i++)
        {
            if (expr[i] == '[') depth++;
            else if (expr[i] == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int FindMatchingParenthesis(string expr, int start)
    {
        int depth = 0;
        for (int i = start; i < expr.Length; i++)
        {
            if (expr[i] == '(') depth++;
            else if (expr[i] == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static object? GetNestedValue(object? obj, string accessPath)
    {
        if (obj is null || string.IsNullOrEmpty(accessPath))
            return obj;

        var i = 0;
        while (i < accessPath.Length)
        {
            var ch = accessPath[i];

            if (ch == '.')
            {
                i++; // skip '.'
                if (i >= accessPath.Length) break;
                var name = ReadName(accessPath, ref i);
                if (string.IsNullOrEmpty(name)) break;
                obj = GetPropertyValue(obj, name);
            }
            else if (ch == '[')
            {
                i++; // skip '['
                var indexStr = ReadUntil(accessPath, ref i, ']'); // i will point at ']' when done
                if (!int.TryParse(indexStr, out var idx))
                {
                    // Only numeric literal indices are supported for bracket access
                    return null;
                }

                obj = GetArrayItem(obj, idx);

                // advance past closing ']'
                if (i < accessPath.Length && accessPath[i] == ']')
                    i++;
            }
            else
            {
                // Support initial property name without leading '.'
                var name = ReadName(accessPath, ref i);
                if (string.IsNullOrEmpty(name))
                    break;

                obj = GetPropertyValue(obj, name);
            }


            if (obj is null) break;
        }

        return UnwrapJToken(obj);
    }

    private static object? GetArrayItem(object obj, int index)
    {
        obj = UnwrapJToken(obj) ?? obj;

        if (obj is JArray jarr)
        {
            if (index >= 0 && index < jarr.Count)
                return UnwrapJToken(jarr[index]);
            return null;
        }

        if (obj is IList list)
        {
            if (index >= 0 && index < list.Count)
                return UnwrapJToken(list[index]);
            return null;
        }

        if (obj is Array arr)
        {
            if (index >= 0 && index < arr.Length)
                return UnwrapJToken(arr.GetValue(index));
            return null;
        }

        // Fall back to IEnumerable traversal (inefficient but safe)
        if (obj is IEnumerable enumerable)
        {
            var i = 0;
            foreach (var item in enumerable)
            {
                if (i == index)
                    return UnwrapJToken(item);
                i++;
            }
            return null;
        }

        return null;
    }

    private static object? GetPropertyValue(object? obj, string propertyKey)
    {
        if (obj is null) return null;

        obj = UnwrapJToken(obj) ?? obj;

        // JObject -> case-insensitive property lookup
        if (obj is JObject jobj)
        {
            var prop = jobj.Property(propertyKey, StringComparison.OrdinalIgnoreCase);
            return prop is null ? null : UnwrapJToken(prop.Value);
        }

        // JArray does not support property access by name (avoid exception)
        if (obj is JArray)
            return null;

        // IDictionary<string, object?>
        if (obj is IDictionary<string, object?> stringDict)
        {
            if (stringDict.TryGetValue(propertyKey, out var val))
                return UnwrapJToken(val);

            // Try case-insensitive
            return stringDict
                .Where(kv => string.Equals(kv.Key, propertyKey, StringComparison.OrdinalIgnoreCase))
                .Select(kv => UnwrapJToken(kv.Value))
                .FirstOrDefault();
        }

        // IDictionary (non-generic)
        if (obj is IDictionary dict)
        {
            if (dict.Contains(propertyKey))
                return UnwrapJToken(dict[propertyKey]);

            // Try case-insensitive string keys
            foreach (DictionaryEntry de in dict)
            {
                if (de.Key is string sk && string.Equals(sk, propertyKey, StringComparison.OrdinalIgnoreCase))
                    return UnwrapJToken(de.Value);
            }
            return null;
        }

        // Reflection on POCOs (case-insensitive)
        var type = obj.GetType();
        var propInfo = type.GetProperty(propertyKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (propInfo != null)
            return UnwrapJToken(propInfo.GetValue(obj));

        // Field fallback (case-insensitive)
        var fieldInfo = type.GetField(propertyKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (fieldInfo != null)
            return UnwrapJToken(fieldInfo.GetValue(obj));

        return null;
    }

    private static object? UnwrapJToken(object? value)
    {
        return value switch
        {
            null => null,
            JValue jv => jv.Value,
            JToken jt when jt.Type == JTokenType.Null => null,
            _ => value,
        };
    }

    private static string ReadName(string s, ref int i)
    {
        var start = i;
        while (i < s.Length)
        {
            var ch = s[i];
            if (ch == '.' || ch == '[' || ch == ']')
                break;
            i++;
        }
        return start == i ? string.Empty : s[start..i];
    }

    private static string ReadUntil(string s, ref int i, char endChar)
    {
        var start = i;
        while (i < s.Length && s[i] != endChar)
        {
            i++;
        }
        // i now points to endChar or s.Length (if not found)
        return s[start..i];
    }
}