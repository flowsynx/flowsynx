using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace FlowSynx.Core.Services;

public class TemplateEngine
{
    private readonly JObject _variables;
    private readonly List<TransformationFunction> _functions;

    public TemplateEngine(JObject variables)
    {
        _variables = variables;
        _functions = new List<TransformationFunction>();
    }

    public void RegisterTransformation(TransformationFunction transformation)
    {
        if (_functions.Any(_ => Equals(transformation.Name)))
            throw new Exception("This function is already defined!");

        _functions.Add(transformation);
    }

    public string Render(string template)
    {
        var result = new StringBuilder();
        var cursor = 0;

        while (cursor < template.Length)
        {
            var startIndex = template.IndexOf("$[", cursor, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                result.Append(template[cursor..]);
                break;
            }

            result.Append(template.Substring(cursor, startIndex - cursor));

            var endIndex = FindClosingBracket(template, startIndex + 2);
            if (endIndex == -1)
            {
                throw new Exception("Unmatched opening placeholder delimiter.");
            }

            var placeholder = template.Substring(startIndex + 2, endIndex - startIndex - 2);
            var resolvedValue = EvaluateExpression(placeholder);
            result.Append(resolvedValue);

            cursor = endIndex + 1;
        }

        return result.ToString();
    }

    private string EvaluateExpression(string expression)
    {
        var parts = expression.Split('|');
        var valueExpression = parts[0].Trim();
        var transformations = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        // Resolve the base value
        object value;

        if (valueExpression.Contains("(") && valueExpression.Contains(")") && !IsVariableReference(valueExpression))
        {
            var (transformationName, arguments) = ParseTransformation(valueExpression);

            // Recursively resolve arguments of the transformation
            for (var i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is string argString)
                {
                    arguments[i] = EvaluateExpression(argString); // Recursively evaluate nested expressions
                }
            }

            var func = _functions.FirstOrDefault(x => x.Name.Equals(transformationName));
            if (func != null)
            {
                // Apply the transformation with arguments
                value = ApplyTransformation(null, func, arguments);
            }
            else
            {
                throw new Exception($"Unknown functions: {transformationName}");
            }
        }
        else
        {
            // Otherwise resolve the expression as a variable or value
            value = ResolveOrEvaluate(valueExpression);
        }

        // Apply transformation pipeline recursively
        foreach (var transformation in transformations)
        {
            var trimmedTransformation = transformation.Trim();
            var (transformationName, arguments) = ParseTransformation(trimmedTransformation);

            // Recursively resolve arguments of the transformation
            for (var i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is string argString)
                {
                    arguments[i] = EvaluateExpression(argString); // Recursively evaluate nested expressions
                }
            }

            var func2 = _functions.FirstOrDefault(x => x.Name.Equals(transformationName));

            if (func2 != null)
            {
                value = ApplyTransformation(value, func2, arguments);
            }
            else
            {
                throw new Exception($"Unknown transformation: {transformationName}");
            }
        }

        return value.ToString() ?? string.Empty;
    }

    private bool IsVariableReference(string expression)
    {
        return expression.StartsWith("variables(");
    }

    private object? ResolveVariablePath(string path)
    {
        var segments = path.Split('.');
        object? current = _variables;

        foreach (var segment in segments)
        {
            var isArrayAccess = segment.Contains("[");
            var baseSegment = isArrayAccess ? segment[..segment.IndexOf('[')] : segment;

            switch (current)
            {
                case JObject jObject:
                    current = jObject[baseSegment];
                    break;
                case IDictionary<string, object> dict:
                    current = dict.TryGetValue(baseSegment, out var value) ? value : null;
                    break;
                default:
                    return null;
            }

            if (isArrayAccess)
            {
                var indexString = segment.Substring(segment.IndexOf('[') + 1, segment.Length - baseSegment.Length - 2);
                if (int.TryParse(indexString, out var index) && current is JArray jArray)
                {
                    current = index >= 0 && index < jArray.Count ? jArray[index] : null;
                }
                else
                {
                    return null;
                }
            }
        }

        return RenderValue(current);
    }

    private object ResolveOrEvaluate(string expression)
    {
        var resolvedExpression = ReplaceVariables(expression);
        if (IsMathExpression(resolvedExpression))
        {
            return EvaluateMathExpression(resolvedExpression);
        }

        return resolvedExpression;
    }

    private string ReplaceVariables(string expression)
    {
        var result = new StringBuilder();
        var cursor = 0;

        while (cursor < expression.Length)
        {
            var startIndex = expression.IndexOf("variables(", cursor, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                result.Append(expression[cursor..]);
                break;
            }

            result.Append(expression.Substring(cursor, startIndex - cursor));

            var endIndex = expression.IndexOf(")", startIndex, StringComparison.Ordinal);
            if (endIndex == -1)
            {
                throw new Exception("Unmatched opening 'variables(' delimiter.");
            }

            var variablePath = expression.Substring(startIndex + 10, endIndex - startIndex - 10);
            var resolvedValue = ResolveVariablePath(variablePath);

            result.Append(resolvedValue);
            cursor = endIndex + 1;
        }

        return result.ToString();
    }

    private bool IsMathExpression(string expression)
    {
        return expression.Contains("+") || expression.Contains("-") || expression.Contains("*") || expression.Contains("/");
    }

    private double EvaluateMathExpression(string expression)
    {
        var sanitizedExpression = expression.Replace(" ", string.Empty);
        return Evaluate(sanitizedExpression);
    }

    private double Evaluate(string expression)
    {
        var values = new Stack<double>();
        var operators = new Stack<char>();

        for (var i = 0; i < expression.Length; i++)
        {
            var currentChar = expression[i];

            if (char.IsDigit(currentChar) || currentChar == '.')
            {
                var number = string.Empty;

                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    number += expression[i];
                    i++;
                }

                values.Push(double.Parse(number));
                i--;
            }
            else switch (currentChar)
            {
                case '(':
                    operators.Push(currentChar);
                    break;
                case ')':
                {
                    while (operators.Peek() != '(')
                    {
                        values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                    }
                    operators.Pop();
                    break;
                }
                default:
                {
                    if ("+-*/".Contains(currentChar))
                    {
                        while (operators.Count > 0 && HasPrecedence(currentChar, operators.Peek()))
                        {
                            values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                        }
                        operators.Push(currentChar);
                    }

                    break;
                }
            }
        }

        while (operators.Count > 0)
        {
            values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
        }

        return values.Pop();
    }

    private bool HasPrecedence(char currentOperator, char stackOperator)
    {
        return currentOperator is '+' or '-' && stackOperator is '*' or '/';
    }

    private double ApplyOperator(char operatorChar, double b, double a)
    {
        return operatorChar switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            '/' => a / b,
            _ => throw new InvalidOperationException($"Unknown operator: {operatorChar}")
        };
    }

    private Tuple<string, List<object>> ParseTransformation(string transformation)
    {
        var openParenIndex = transformation.IndexOf('(');
        var closeParenIndex = transformation.IndexOf(')');

        if (openParenIndex == -1 || closeParenIndex == -1)
        {
            return Tuple.Create(transformation, new List<object>());
        }

        var functionName = transformation[..openParenIndex].Trim();
        var argsString = transformation.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
        var args = ParseArguments(argsString);

        return Tuple.Create(functionName, args);
    }

    private List<object> ParseArguments(string argsString)
    {
        var segments = argsString.Split(',');
        return segments.Select(segment => segment.Trim()).Cast<object>().ToList();
    }

    private object ApplyTransformation(object? value, TransformationFunction func, List<object> arguments)
    {
        if (value != null)
        {
            arguments.Insert(0, value);
        }

        ValidateArguments(func, arguments);

        return func.Apply(arguments);
    }

    private void ValidateArguments(TransformationFunction func, List<object> arguments)
    {
        if (func.ExpectedArgumentCount != arguments.Count)
        {
            throw new ArgumentException($"Transformation '{func.Name}' expects {func.ExpectedArgumentCount} arguments but got {arguments.Count}.");
        }
    }

    /// <summary>
    /// Finds the matching closing bracket for a placeholder.
    /// </summary>
    private int FindClosingBracket(string input, int startIndex)
    {
        var depth = 1;
        for (var i = startIndex; i < input.Length; i++)
        {
            if (input[i] == '[') depth++;
            if (input[i] == ']') depth--;

            if (depth == 0) return i;
        }

        return -1; // No matching closing bracket found
    }

    private string RenderValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string => value.ToString(),
            bool => value.ToString().ToLower(),
            _ => JsonConvert.SerializeObject(value)
        };
    }
}

public class TransformationFunction
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int ExpectedArgumentCount { get; set; }
    public required Func<List<object>, object> Apply { get; set; }
}