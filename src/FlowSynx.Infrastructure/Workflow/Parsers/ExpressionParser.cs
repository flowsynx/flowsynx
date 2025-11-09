using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParser : IExpressionParser
{
    private readonly Dictionary<string, object?> _outputs;
    private readonly Dictionary<string, object?> _variables;

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

                // If entire expression is just $[...], return directly
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
        // ternary ? :
        if (Regex.IsMatch(inner, @"\?.*:"))
            return EvaluateConditionalExpression(inner);

        // boolean logic or comparisons
        if (Regex.IsMatch(inner, @"[=!<>]=?|&&|\|\|"))
            return EvaluateBooleanExpression(inner);

        // arithmetic
        if (Regex.IsMatch(inner, @"[\+\-\*/%]"))
            return EvaluateArithmeticExpression(inner);

        // variable or output
        return ResolveInnerExpression(inner);
    }

    private object? EvaluateConditionalExpression(string expr)
    {
        int questionIdx = -1, colonIdx = -1, depth = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            if (expr[i] == '?') { if (depth == 0 && questionIdx == -1) questionIdx = i; depth++; }
            else if (expr[i] == ':') { depth--; if (depth == 0 && colonIdx == -1) colonIdx = i; }
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
        if (expr.StartsWith("!"))
            return !EvaluateBooleanExpression(expr[1..]);

        // parentheses
        if (expr.StartsWith("(") && expr.EndsWith(")"))
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

        // Replace variable expressions
        expr = Regex.Replace(expr, @"(Outputs|Variables)\([^)]*\)", m =>
        {
            var val = ResolveInnerExpression(m.Value);
            return val?.ToString() ?? "0";
        });

        try
        {
            using var dt = new DataTable();
            var value = dt.Compute(expr, null);
            return Convert.ToDouble(value);
        }
        catch
        {
            // if numeric conversion fails, fallback to string/variable
            return ResolveLiteralOrValue(expr);
        }
    }

    private string ReplaceEmbeddedExpressions(string expr)
    {
        var matches = Regex.Matches(expr, @"\$\[[^\]]+\]");
        foreach (Match m in matches)
        {
            string sub = m.Value;
            string inner = sub[2..^1];
            object? val = ResolveInnerOrConditionalOrMath(inner);
            expr = expr.Replace(sub, val?.ToString() ?? "null");
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

        if (str.StartsWith("'") && str.EndsWith("'"))
            return StripQuotes(str);

        if (str.StartsWith("Outputs(") || str.StartsWith("Variables("))
            return ResolveInnerExpression(str);

        if (double.TryParse(str, out double num)) return num;
        if (bool.TryParse(str, out bool b)) return b;

        return str;
    }

    private object? EvaluateExpression(string inner)
    {
        // Handle ternary expressions
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

        // handle normal boolean/comparison cases...
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

        // detect ternary or comparison
        if (ContainsOperator(inner))
        {
            return EvaluateExpression(inner);
        }

        // handle literal values safely
        if (IsLiteral(inner))
        {
            return ParseLiteral(inner);
        }

        string sourceType;
        if (inner.StartsWith("Outputs("))
            sourceType = "Outputs";
        else if (inner.StartsWith("Variables("))
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
            (inner.StartsWith("'") && inner.EndsWith("'")) ||
            (inner.StartsWith("\"") && inner.EndsWith("\"")) ||
            double.TryParse(inner, out _) ||
            bool.TryParse(inner, out _) ||
            inner.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    private static object? ParseLiteral(string inner)
    {
        inner = inner.Trim();

        if ((inner.StartsWith("'") && inner.EndsWith("'")) ||
            (inner.StartsWith("\"") && inner.EndsWith("\"")))
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
        if (expr.StartsWith("Outputs(") || expr.StartsWith("Variables("))
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
        if (obj == null) return null;

        int i = 0;
        while (i < accessPath.Length)
        {
            if (accessPath[i] == '.')
            {
                i++;
                string prop = ReadName(accessPath, ref i);
                obj = GetPropertyValue(obj, prop);
            }
            else if (accessPath[i] == '[')
            {
                i++;
                string idxStr = ReadUntil(accessPath, ref i, ']');
                if (int.TryParse(idxStr, out int idx))
                    obj = GetArrayItem(obj, idx);
                i++;
            }
            else i++;
        }

        return UnwrapJToken(obj);
    }

    private static string ReadName(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        return s.Substring(start, i - start);
    }

    private static string ReadUntil(string s, ref int i, char endChar)
    {
        int start = i;
        while (i < s.Length && s[i] != endChar) i++;
        return s.Substring(start, i - start);
    }

    private static object? GetArrayItem(object obj, int index)
    {
        if (obj is IList list && index >= 0 && index < list.Count) return list[index];
        if (obj is JArray jArr && index >= 0 && index < jArr.Count) return jArr[index];
        return null;
    }

    private static object? GetPropertyValue(object? obj, string propertyKey)
    {
        if (obj is JObject jObj)
            return jObj.TryGetValue(propertyKey, StringComparison.OrdinalIgnoreCase, out var token) ? token : null;
        if (obj is JToken jToken)
            return jToken[propertyKey];

        var propInfo = obj?.GetType().GetProperty(propertyKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return propInfo?.GetValue(obj);
    }

    private static object? UnwrapJToken(object? value) => value switch
    {
        JValue jValue => jValue.Value,
        _ => value
    };
}