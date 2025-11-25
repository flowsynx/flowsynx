using FlowSynx.Domain;
using FlowSynx.Infrastructure.Secrets;
using FlowSynx.Infrastructure.Workflow.Expressions.Functions;
using FlowSynx.Infrastructure.Workflow.Expressions.SourceResolver;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using System.Reflection;

namespace FlowSynx.Infrastructure.Workflow.Expressions;

public class ExpressionParser : IExpressionParser
{
    private readonly Dictionary<string, ISourceResolver> _sourceResolvers;
    private readonly Dictionary<string, IFunctionEvaluator> _functionEvaluators;

    public ExpressionParser(
        Dictionary<string, object?> outputs,
        Dictionary<string, object?> variables,
        ISecretFactory? secretFactory = null,
        IEnumerable<IFunctionEvaluator>? customFunctions = null)
    {
        // Initialize source resolvers with prefix mapping
        _sourceResolvers = new Dictionary<string, ISourceResolver>(StringComparer.OrdinalIgnoreCase)
        {
            { "Outputs", new DictionarySourceResolver(outputs, "Outputs") },
            { "Variables", new DictionarySourceResolver(variables, "Variables") }
        };

        // Add Secrets resolver if a secret factory is provided and has a default provider
        if (secretFactory != null)
        {
            var secretProvider = secretFactory.GetDefaultProvider();
            if (secretProvider != null)
            {
                _sourceResolvers.Add("Secrets", new SecretsSourceResolver(secretProvider));
            }
        }

        // Initialize built-in function evaluators
        _functionEvaluators = new Dictionary<string, IFunctionEvaluator>(StringComparer.OrdinalIgnoreCase);

        // Register built-in functions
        RegisterFunction(new MinFunction());
        RegisterFunction(new MaxFunction());
        RegisterFunction(new SumFunction());
        RegisterFunction(new AvgFunction());
        RegisterFunction(new CountFunction());
        RegisterFunction(new ContainsFunction());
        RegisterFunction(new LengthFunction());
        RegisterFunction(new GuidFunction());
        RegisterFunction(new NowFunction());
        RegisterFunction(new NowUtcFunction());
        RegisterFunction(new IsNullFunction());

        // Register custom functions if provided
        if (customFunctions != null)
        {
            foreach (var function in customFunctions)
            {
                RegisterFunction(function);
            }
        }
    }

    /// <summary>
    /// Registers a function evaluator
    /// </summary>
    public void RegisterFunction(IFunctionEvaluator function)
    {
        if (function == null)
            throw new ArgumentNullException(nameof(function));

        _functionEvaluators[function.Name] = function;
    }

    /// <summary>
    /// Unregisters a function evaluator by name
    /// </summary>
    public bool UnregisterFunction(string functionName)
    {
        return _functionEvaluators.Remove(functionName);
    }

    public async Task<object?> ParseAsync(string? expression, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        return await ResolveExpression(expression, cancellationToken);
    }

    private async Task<object?> ResolveExpression(string expr, CancellationToken cancellationToken)
    {
        int i = 0;
        while (i < expr.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i + 2 < expr.Length && expr[i] == '$' && expr[i + 1] == '[')
            {
                int end = FindMatchingBracket(expr, i + 1);
                if (end == -1)
                    throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                        $"Unbalanced brackets in expression: {expr}");

                string inner = expr.Substring(i + 2, end - i - 2).Trim();
                object? resolved = await ResolveInnerOrConditionalOrMath(inner, cancellationToken);

                if (expr.Trim() == expr.Substring(i, end - i + 1))
                    return resolved;

                expr = expr.Substring(0, i) + (resolved?.ToString() ?? string.Empty) + expr.Substring(end + 1);
                i += (resolved?.ToString() ?? string.Empty).Length;
            }
            else i++;
        }
        return expr;
    }

    private async Task<object?> ResolveInnerOrConditionalOrMath(string inner, CancellationToken cancellationToken)
    {
        if (inner.Contains('?') && inner.Contains(':'))
            return await EvaluateConditionalExpression(inner, cancellationToken);

        if (await TryEvaluateFunctionalExpression(inner, cancellationToken) is var fnResult && fnResult.HasValue)
            return fnResult.Value;

        if (ContainsOperator(inner))
            return await EvaluateBooleanExpression(inner, cancellationToken);

        if (inner.IndexOfAny(new[] { '+', '-', '*', '/', '%' }) >= 0)
            return await EvaluateArithmeticExpression(inner, cancellationToken);

        return await ResolveInnerExpression(inner, cancellationToken);
    }

    private bool TryGetSourceResolver(string expr, int pos, out ISourceResolver? resolver, out string prefix)
    {
        resolver = null;
        prefix = string.Empty;

        foreach (var kvp in _sourceResolvers)
        {
            string testPrefix = $"{kvp.Key}(";
            if (expr.Substring(pos).StartsWith(testPrefix, StringComparison.OrdinalIgnoreCase))
            {
                resolver = kvp.Value;
                prefix = testPrefix;
                return true;
            }
        }

        return false;
    }

    private bool StartsWithAnyPrefix(string expr)
    {
        return _sourceResolvers.Keys.Any(key =>
            expr.StartsWith($"{key}(", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(bool HasValue, object? Value)> TryEvaluateFunctionalExpression(string inner, CancellationToken cancellationToken)
    {
        inner = inner.Trim();
        int parenIdx = inner.IndexOf('(');
        if (parenIdx <= 0) return (false, null);

        string name = inner.Substring(0, parenIdx).Trim();

        // Check if function is registered
        if (!_functionEvaluators.TryGetValue(name, out var evaluator))
            return (false, null);

        int endParen = FindMatchingParenthesis(inner, parenIdx);
        if (endParen == -1)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"Unbalanced parentheses in functional expression: {inner}");

        // Check if there's content after the function call (e.g., arithmetic operators)
        // If so, this isn't a pure function expression and should be handled elsewhere
        if (endParen < inner.Length - 1)
        {
            string remainder = inner.Substring(endParen + 1).Trim();
            if (!string.IsNullOrEmpty(remainder))
                return (false, null);
        }

        string argsSegment = inner.Substring(parenIdx + 1, endParen - parenIdx - 1);
        var args = SplitArguments(argsSegment);

        var evaluatedArgs = new List<object?>();
        foreach (var arg in args)
        {
            evaluatedArgs.Add(await EvaluateFunctionalArgument(arg, cancellationToken));
        }

        // Use the registered evaluator
        var result = evaluator.Evaluate(evaluatedArgs);
        return (true, result);
    }

    private static List<string> SplitArguments(string argsSegment)
    {
        if (string.IsNullOrWhiteSpace(argsSegment))
            return new List<string>();

        var list = new List<string>();
        int depth = 0;
        bool inQuotes = false;
        char quoteChar = '\0';
        int start = 0;

        for (int i = 0; i < argsSegment.Length; i++)
        {
            char c = argsSegment[i];

            HandleQuotes(c, ref inQuotes, ref quoteChar);

            if (inQuotes)
                continue;

            HandleDepth(c, ref depth);

            if (c == ',' && depth == 0)
            {
                AddSegment(list, argsSegment, start, i);
                start = i + 1;
            }
        }

        if (start < argsSegment.Length)
            AddSegment(list, argsSegment, start, argsSegment.Length);

        return list;
    }

    private static void HandleQuotes(char c, ref bool inQuotes, ref char quoteChar)
    {
        if (c != '\'' && c != '"') return;

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

    private static void HandleDepth(char c, ref int depth)
    {
        switch (c)
        {
            case '(':
            case '[':
                depth++;
                break;
            case ')':
            case ']':
                depth--;
                break;
        }
    }

    private static void AddSegment(List<string> list, string str, int start, int end)
    {
        var segment = str.Substring(start, end - start).Trim();
        if (!string.IsNullOrEmpty(segment))
            list.Add(segment);
    }

    private async Task<object?> EvaluateFunctionalArgument(string arg, CancellationToken cancellationToken)
    {
        arg = arg.Trim();

        if (await TryEvaluateFunctionalExpression(arg, cancellationToken) is var fnValue && fnValue.HasValue)
            return fnValue.Value;

        if (StartsWithAnyPrefix(arg) || ContainsOperator(arg) ||
            arg.Contains("$[") || arg.Contains('?') || arg.Contains(':'))
        {
            return await ParseAsync($"$[{arg}]", cancellationToken);
        }

        var lit = await ResolveLiteralOrValue(arg, cancellationToken);
        return lit;
    }

    private async Task<object?> EvaluateConditionalExpression(string expr, CancellationToken cancellationToken)
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

        bool conditionResult = await EvaluateBooleanExpression(condition, cancellationToken);
        return await ParseAsync($"$[{(conditionResult ? truePart : falsePart)}]", cancellationToken);
    }

    private async Task<bool> EvaluateBooleanExpression(string expr, CancellationToken cancellationToken)
    {
        expr = expr.Trim();
        expr = await ReplaceEmbeddedExpressions(expr, cancellationToken);

        // Handle negation
        if (expr.StartsWith('!'))
            return !await EvaluateBooleanExpression(expr[1..], cancellationToken);

        // Handle parentheses
        if (expr.StartsWith('(') && expr.EndsWith(')'))
            return await EvaluateBooleanExpression(expr[1..^1], cancellationToken);

        // Handle logical AND
        if (expr.Contains("&&"))
            return await EvaluateLogicalOperator(expr, "&&", cancellationToken, shortCircuitValue: false);

        // Handle logical OR
        if (expr.Contains("||"))
            return await EvaluateLogicalOperator(expr, "||", cancellationToken, shortCircuitValue: true);

        // Handle comparison operators
        string[] ops = { ">=", "<=", "==", "!=", ">", "<" };
        foreach (var op in ops)
        {
            int idx = expr.IndexOf(op, StringComparison.Ordinal);
            if (idx > 0)
            {
                string left = expr[..idx].Trim();
                string right = expr[(idx + op.Length)..].Trim();
                object? lVal = await EvaluateArithmeticExpression(left, cancellationToken);
                object? rVal = await EvaluateArithmeticExpression(right, cancellationToken);
                return Compare(lVal, rVal, op);
            }
        }

        // Handle literal boolean
        if (bool.TryParse(expr, out bool boolVal))
            return boolVal;

        throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid boolean expression: {expr}");
    }

    // Helper method for evaluating AND/OR expressions
    private async Task<bool> EvaluateLogicalOperator(string expr, string op, CancellationToken cancellationToken, bool shortCircuitValue)
    {
        var parts = expr.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            bool result = await EvaluateBooleanExpression(part, cancellationToken);
            if (result == shortCircuitValue)
                return shortCircuitValue; // Short-circuit
        }
        return !shortCircuitValue;
    }

    private async Task<object?> EvaluateArithmeticExpression(string expr, CancellationToken cancellationToken)
    {
        expr = await ReplaceEmbeddedExpressions(expr, cancellationToken);
        expr = await ReplaceVariables(expr, cancellationToken);
        expr = await ReplaceFunctions(expr, cancellationToken);

        try
        {
            using var dt = new DataTable();
            var value = dt.Compute(expr, null);
            return Convert.ToDouble(value);
        }
        catch
        {
            return await ResolveLiteralOrValue(expr, cancellationToken);
        }
    }

    private async Task<string> ReplaceFunctions(string expr, CancellationToken cancellationToken)
    {
        int pos = 0;

        while (pos < expr.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryMatchFunction(expr, pos, out string funcName, out int parenStart, out int parenEnd))
            {
                pos++;
                continue;
            }

            _ = expr.Substring(pos, parenEnd - pos + 1);
            string argsSegment = expr.Substring(parenStart + 1, parenEnd - parenStart - 1);
            var evaluatedArgs = await EvaluateArguments(argsSegment, cancellationToken);

            var result = _functionEvaluators[funcName].Evaluate(evaluatedArgs);
            string replacement = result?.ToString() ?? "0";

            expr = expr.Substring(0, pos) + replacement + expr.Substring(parenEnd + 1);
            pos += replacement.Length;
        }

        return expr;
    }

    private bool TryMatchFunction(string expr, int pos, out string funcName, out int parenStart, out int parenEnd)
    {
        foreach (var name in _functionEvaluators.Keys)
        {
            string pattern = $"{name}(";
            if (pos + pattern.Length <= expr.Length &&
                expr.Substring(pos, pattern.Length).Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                funcName = name;
                parenStart = pos + name.Length;
                parenEnd = FindMatchingParenthesis(expr, parenStart);
                if (parenEnd != -1)
                {
                    return true;
                }
            }
        }

        funcName = string.Empty;
        parenStart = -1;
        parenEnd = -1;
        return false;
    }

    private async Task<List<object?>> EvaluateArguments(string argsSegment, CancellationToken cancellationToken)
    {
        var args = SplitArguments(argsSegment);
        var evaluatedArgs = new List<object?>();

        foreach (var arg in args)
        {
            evaluatedArgs.Add(await EvaluateFunctionalArgument(arg, cancellationToken));
        }

        return evaluatedArgs;
    }

    private async Task<string> ReplaceVariables(string expr, CancellationToken cancellationToken)
    {
        int pos = 0;

        while (pos < expr.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetSourceResolver(expr, pos, out var resolver, out var prefix))
            {
                pos++;
                continue;
            }

            int start = pos + prefix.Length - 1;
            int end = FindMatchingParenthesis(expr, start);
            if (end == -1)
            {
                pos++;
                continue;
            }

            string keyExpr = expr.Substring(start + 1, end - start - 1);
            string resolvedKey = StripQuotes(await ResolveTopLevelExpression(keyExpr, cancellationToken));
            object? rootValue = await resolver!.ResolveAsync(resolvedKey, cancellationToken);

            int pathStart = end + 1;
            string accessPath = ScanAccessPath(expr, pathStart, out int scan);

            object? finalValue = string.IsNullOrEmpty(accessPath)
                ? rootValue
                : GetNestedValue(rootValue, accessPath);

            string replacement = finalValue?.ToString() ?? "0";

            expr = expr.Substring(0, pos) + replacement + expr.Substring(scan);
            pos += replacement.Length;
        }

        return expr;
    }

    private static string ScanAccessPath(string expr, int start, out int end)
    {
        int pos = start;
        while (pos < expr.Length)
        {
            char c = expr[pos];
            if (c == '.')
            {
                pos++;
                pos = ScanIdentifier(expr, pos);
            }
            else if (c == '[')
            {
                pos++;
                pos = ScanBracket(expr, pos);
                if (pos == -1) break;
            }
            else
            {
                break;
            }
        }
        end = pos;
        return pos > start ? expr.Substring(start, pos - start) : string.Empty;
    }

    private static int ScanIdentifier(string expr, int pos)
    {
        while (pos < expr.Length)
        {
            char c = expr[pos];
            if (char.IsLetterOrDigit(c) || c == '_')
                pos++;
            else
                break;
        }
        return pos;
    }

    private static int ScanBracket(string expr, int pos)
    {
        while (pos < expr.Length && expr[pos] != ']') pos++;
        return pos < expr.Length && expr[pos] == ']' ? pos + 1 : -1;
    }

    private async Task<string> ReplaceEmbeddedExpressions(string expr, CancellationToken cancellationToken)
    {
        int pos = 0;
        while (pos < expr.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int start = expr.IndexOf("$[", pos, StringComparison.Ordinal);
            if (start == -1) break;

            int end = FindMatchingBracket(expr, start + 1);
            if (end == -1)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                    $"Unbalanced brackets in expression: {expr}");

            string inner = expr.Substring(start + 2, end - start - 2).Trim();
            object? val = await ResolveInnerOrConditionalOrMath(inner, cancellationToken);
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

    private async Task<object?> ResolveLiteralOrValue(string str, CancellationToken cancellationToken)
    {
        str = str.Trim();

        if (str.StartsWith('\'') && str.EndsWith('\'')) return StripQuotes(str);

        if (StartsWithAnyPrefix(str))
            return await ResolveInnerExpression(str, cancellationToken);

        if (double.TryParse(str, out double num)) return num;
        if (bool.TryParse(str, out bool b)) return b;

        return str;
    }

    private async Task<object?> EvaluateExpression(string inner, CancellationToken cancellationToken)
    {
        if (inner.Contains('?') && inner.Contains(':'))
        {
            var parts = SplitTernary(inner);
            if (parts == null)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid expression: {inner}");

            var condition = parts.Value.condition.Trim();
            var trueExpr = parts.Value.ifTrue.Trim();
            var falseExpr = parts.Value.ifFalse.Trim();

            var conditionResult = await EvaluateBooleanExpression(condition, cancellationToken);
            return conditionResult
                ? await ParseAsync($"$[{trueExpr}]", cancellationToken)
                : await ParseAsync($"$[{falseExpr}]", cancellationToken);
        }

        return await EvaluateBooleanExpression(inner, cancellationToken);
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

    private async Task<object?> ResolveInnerExpression(string inner, CancellationToken cancellationToken)
    {
        inner = inner.Trim();

        if (await TryEvaluateFunctionalExpression(inner, cancellationToken) is var fnValue && fnValue.HasValue)
            return fnValue.Value;

        if (ContainsOperator(inner))
            return await EvaluateExpression(inner, cancellationToken);

        if (IsLiteral(inner))
            return ParseLiteral(inner);

        ISourceResolver? resolver = null;
        string? matchedPrefix = null;

        foreach (var kvp in _sourceResolvers)
        {
            string prefix = $"{kvp.Key}(";
            if (inner.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                resolver = kvp.Value;
                matchedPrefix = prefix;
                break;
            }
        }

        if (resolver == null || matchedPrefix == null)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Invalid expression: {inner}");

        int startKey = matchedPrefix.Length;
        int endKey = FindMatchingParenthesis(inner, startKey - 1);
        if (endKey == -1)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, $"Unbalanced parentheses: {inner}");

        string keyExpr = inner.Substring(startKey, endKey - startKey).Trim();
        string accessPath = inner.Substring(endKey + 1).Trim();

        string resolvedKey = StripQuotes(await ResolveTopLevelExpression(keyExpr, cancellationToken));
        object? value = await resolver.ResolveAsync(resolvedKey, cancellationToken);

        if (!string.IsNullOrEmpty(accessPath))
            value = GetNestedValue(value, accessPath);

        return value;
    }

    private static bool IsLiteral(string inner)
    {
        if (string.IsNullOrWhiteSpace(inner))
            return false;

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

    private async Task<string> ResolveTopLevelExpression(string expr, CancellationToken cancellationToken)
    {
        expr = expr.Trim();
        if (StartsWithAnyPrefix(expr))
            return (await ParseAsync($"$[{expr}]", cancellationToken))?.ToString() ?? string.Empty;

        return expr;
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

        int i = 0;
        while (i < accessPath.Length && obj != null)
        {
            obj = accessPath[i] switch
            {
                '.' => HandleProperty(obj, accessPath, ref i),
                '[' => HandleArray(obj, accessPath, ref i),
                _ => HandleProperty(obj, accessPath, ref i)
            };
        }

        return UnwrapJToken(obj);
    }

    private static object? HandleProperty(object obj, string path, ref int i)
    {
        i = path[i] == '.' ? i + 1 : i; // skip the dot if present
        var name = ReadName(path, ref i);
        return string.IsNullOrEmpty(name) ? null : GetPropertyValue(obj, name);
    }

    private static object? HandleArray(object obj, string path, ref int i)
    {
        i++; // skip the '['
        var indexStr = ReadUntil(path, ref i, ']');
        if (!int.TryParse(indexStr, out var idx))
            return null;

        var item = GetArrayItem(obj, idx);

        if (i < path.Length && path[i] == ']')
            i++; // skip closing ']'

        return item;
    }

    private static object? GetArrayItem(object obj, int index)
    {
        obj = UnwrapJToken(obj) ?? obj;

        if (index < 0)
            return null;

        switch (obj)
        {
            case JArray jarr when index < jarr.Count:
                return UnwrapJToken(jarr[index]);
            case IList list when index < list.Count:
                return UnwrapJToken(list[index]);
            case Array arr when index < arr.Length:
                return UnwrapJToken(arr.GetValue(index));
            case IEnumerable enumerable:
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (i == index)
                        return UnwrapJToken(item);
                    i++;
                }
                break;
            default:
                return null;
        }

        return null;
    }

    private static object? GetPropertyValue(object? obj, string propertyKey)
    {
        if (obj is null) return null;

        obj = UnwrapJToken(obj) ?? obj;

        switch (obj)
        {
            case JObject jobj:
                return UnwrapJToken(jobj.Property(propertyKey, StringComparison.OrdinalIgnoreCase)?.Value);

            case JArray:
                return null;

            case IDictionary<string, object?> stringDict:
                if (stringDict.TryGetValue(propertyKey, out var val))
                    return UnwrapJToken(val);

                return stringDict
                    .FirstOrDefault(kv => string.Equals(kv.Key, propertyKey, StringComparison.OrdinalIgnoreCase))
                    .Value
                    is { } matchedVal
                    ? UnwrapJToken(matchedVal)
                    : null;

            case IDictionary dict:
                if (dict.Contains(propertyKey))
                    return UnwrapJToken(dict[propertyKey]);

                foreach (DictionaryEntry de in dict)
                {
                    if (de.Key is string sk && string.Equals(sk, propertyKey, StringComparison.OrdinalIgnoreCase))
                        return UnwrapJToken(de.Value);
                }
                return null;

            default:
                var type = obj.GetType();

                var propInfo = type.GetProperty(propertyKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (propInfo != null)
                    return UnwrapJToken(propInfo.GetValue(obj));

                var fieldInfo = type.GetField(propertyKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (fieldInfo != null)
                    return UnwrapJToken(fieldInfo.GetValue(obj));

                return null;
        }
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
        return s[start..i];
    }
}