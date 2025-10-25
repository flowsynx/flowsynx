using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;

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
        if (expression == null) return null;
        return ResolveExpression(expression);
    }

    private object? ResolveExpression(string expr)
    {
        int i = 0;
        object? result = expr;

        while (i < expr.Length)
        {
            if (i + 2 < expr.Length && expr[i] == '$' && expr[i + 1] == '[')
            {
                int end = FindMatchingBracket(expr, i + 1);
                if (end == -1)
                    throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                        $"Unbalanced brackets in expression: {expr}");

                string inner = expr.Substring(i + 2, end - i - 2).Trim();
                object? resolved = ResolveInnerExpression(inner);

                // If the entire expression is just this $[...], return object directly
                if (expr.Trim() == expr.Substring(i, end - i + 1))
                    return resolved;

                // Otherwise, replace substring with string value
                expr = expr.Substring(0, i) + (resolved?.ToString() ?? string.Empty) + expr.Substring(end + 1);
                i += (resolved?.ToString() ?? string.Empty).Length;
            }
            else
            {
                i++;
            }
        }

        return expr;
    }

    private object? ResolveInnerExpression(string inner)
    {
        inner = inner.Trim();

        // Parse sourceType (Outputs or Variables)
        string sourceType;
        if (inner.StartsWith("Outputs("))
            sourceType = "Outputs";
        else if (inner.StartsWith("Variables("))
            sourceType = "Variables";
        else
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"Invalid expression: {inner}");

        int startKey = sourceType.Length + 1;
        int endKey = FindMatchingParenthesis(inner, startKey - 1);
        if (endKey == -1)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"Unbalanced parentheses: {inner}");

        string keyExpr = inner.Substring(startKey, endKey - startKey).Trim();
        string accessPath = inner.Substring(endKey + 1).Trim();

        // Recursively resolve key even if it's another Outputs(...) or Variables(...)
        string resolvedKey = StripQuotes(ResolveTopLevelExpression(keyExpr));

        object? value = ResolveSourceValue(sourceType, resolvedKey);

        // Apply access path if present
        if (!string.IsNullOrEmpty(accessPath))
        {
            value = GetNestedValue(value, accessPath);
        }

        return value;
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
        var dict = sourceType.Equals("Outputs", System.StringComparison.OrdinalIgnoreCase) ? _outputs : _variables;

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
                i++; // skip ']'
            }
            else
            {
                i++;
            }
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
        if (obj is JObject jObj) return jObj.TryGetValue(propertyKey, System.StringComparison.OrdinalIgnoreCase, out var token) ? token : null;
        if (obj is JToken jToken) return jToken[propertyKey];

        var propInfo = obj?.GetType().GetProperty(propertyKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return propInfo?.GetValue(obj);
    }

    private static JToken? TryParseJson(string str)
    {
        try { return JToken.Parse(str); }
        catch { return null; }
    }

    private static object? UnwrapJToken(object? value) => value switch
    {
        JValue jValue => jValue.Value,
        _ => value
    };
}