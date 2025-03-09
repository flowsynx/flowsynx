//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System.Text;

//namespace FlowSynx.Core.Features.Workflow;

//public class WorkflowTemplateEngine
//{
//    private readonly JObject _variables;
//    private readonly IDictionary<string, WorkflowFunction> _functions;
//    private readonly IDictionary<string, object?> _results;

//    public WorkflowTemplateEngine(JObject variables)
//    {
//        _variables = variables;
//        _functions = new Dictionary<string, WorkflowFunction>();
//        _results = new Dictionary<string, object?>();
//    }

//    public void RegisterFunction(string name, WorkflowFunction function)
//    {
//        if (_functions.ContainsKey(name))
//            throw new Exception("This function is already defined!");

//        _functions.Add(name, function);
//    }

//    public void RegisterResults(Dictionary<string, object?> results)
//    {
//        foreach (var keyValue in results)
//        {
//            RegisterResult(keyValue.Key, keyValue.Value);
//        }
//    }

//    public void RegisterResult(string name, object? result)
//    {
//        if (!_results.ContainsKey(name))
//            _results.Add(name, JsonConvert.SerializeObject(result));
//    }

//    public string Render(string template)
//    {
//        var result = new StringBuilder();
//        var cursor = 0;

//        while (cursor < template.Length)
//        {
//            var startIndex = template.IndexOf("$[", cursor, StringComparison.Ordinal);
//            if (startIndex == -1)
//            {
//                result.Append(template[cursor..]);
//                break;
//            }

//            result.Append(template.Substring(cursor, startIndex - cursor));

//            var endIndex = FindClosingBracket(template, startIndex + 2);
//            if (endIndex == -1)
//            {
//                throw new Exception("Unmatched opening placeholder delimiter.");
//            }

//            var placeholder = template.Substring(startIndex + 2, endIndex - startIndex - 2);
//            var resolvedValue = EvaluateExpression(placeholder);
//            result.Append(resolvedValue);

//            cursor = endIndex + 1;
//        }

//        return result.ToString();
//    }

//    private string EvaluateExpression(string expression)
//    {
//        var parts = expression.Split('|');
//        var valueExpression = parts[0].Trim();
//        var transformations = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

//        var value = ResolveOrEvaluate(valueExpression);

//        foreach (var transformation in transformations)
//        {
//            var trimmedTransformation = transformation.Trim();
//            var (transformationName, arguments) = ParseTransformation(trimmedTransformation);

//            for (var i = 0; i < arguments.Count; i++)
//            {
//                if (arguments[i] is string argString)
//                {
//                    arguments[i] = EvaluateExpression(argString);
//                }
//            }

//            if (_functions.TryGetValue(transformationName, out var func))
//            {
//                value = ApplyTransformation(value, func, arguments);
//            }
//            else
//            {
//                throw new Exception($"Unknown transformation: {transformationName}");
//            }
//        }

//        if (value == null)
//            return string.Empty;

//        return value.ToString() ?? string.Empty;
//    }

//    private object? ResolveOrEvaluate(string expression)
//    {
//        if (IsVariableReference(expression))
//        {
//            return ResolveVariable(expression);
//        }

//        if (IsReference(expression))
//        {
//            return ResolveAndEvaluateReference(expression);
//        }

//        var resolvedExpression = ReplaceVariables(expression);
//        if (IsMathExpression(resolvedExpression))
//        {
//            return EvaluateMathExpression(resolvedExpression);
//        }

//        return resolvedExpression;
//    }

//    private bool IsVariableReference(string expression)
//    {
//        return expression.StartsWith("variables(") && expression.EndsWith(")");
//    }

//    private bool IsReference(string expression)
//    {
//        return expression.StartsWith("references(") && expression.EndsWith(")");
//    }

//    private object? ResolveVariable(string expression)
//    {
//        var varName = expression.Substring(10, expression.Length - 11);
//        return ResolveVariablePath(varName);
//    }

//    private object? ResolveAndEvaluateReference(string expression)
//    {
//        var resultName = expression.Substring(11, expression.Length - 12);
//        var evaluatedResultName = EvaluateExpression(resultName);

//        if (!_results.TryGetValue(evaluatedResultName, out var result))
//            throw new Exception($"Unknown reference: {evaluatedResultName}");

//        if (result is string resultTemplate)
//            return Render(resultTemplate);

//        return result;
//    }

//    private object? ResolveVariablePath(string path)
//    {
//        var segments = path.Split('.');
//        object? current = _variables;

//        foreach (var segment in segments)
//        {
//            var isArrayAccess = segment.Contains("[");
//            var baseSegment = isArrayAccess ? segment[..segment.IndexOf('[')] : segment;

//            switch (current)
//            {
//                case JObject jObject:
//                    current = jObject[baseSegment];
//                    break;
//                case IDictionary<string, object> dict:
//                    current = dict.TryGetValue(baseSegment, out var value) ? value : null;
//                    break;
//                default:
//                    return null;
//            }

//            if (isArrayAccess)
//            {
//                var indexString = segment.Substring(segment.IndexOf('[') + 1, segment.Length - baseSegment.Length - 2);
//                if (int.TryParse(indexString, out var index) && current is JArray jArray)
//                {
//                    current = index >= 0 && index < jArray.Count ? jArray[index] : null;
//                }
//                else
//                {
//                    return null;
//                }
//            }
//        }

//        return current;
//    }

//    private string ReplaceVariables(string expression)
//    {
//        var result = new StringBuilder();
//        var cursor = 0;

//        while (cursor < expression.Length)
//        {
//            var startIndex = expression.IndexOf("variables(", cursor, StringComparison.Ordinal);
//            if (startIndex == -1)
//            {
//                result.Append(expression[cursor..]);
//                break;
//            }

//            result.Append(expression.Substring(cursor, startIndex - cursor));

//            var endIndex = expression.IndexOf(")", startIndex, StringComparison.Ordinal);
//            if (endIndex == -1)
//            {
//                throw new Exception("Unmatched opening 'variables(' delimiter.");
//            }

//            var variablePath = expression.Substring(startIndex + 10, endIndex - startIndex - 10);
//            var resolvedValue = ResolveVariable(variablePath);

//            result.Append(resolvedValue);
//            cursor = endIndex + 1;
//        }

//        return result.ToString();
//    }

//    private bool IsMathExpression(string expression)
//    {
//        return expression.Contains("+") || expression.Contains("-") || expression.Contains("*") || expression.Contains("/");
//    }

//    private double EvaluateMathExpression(string expression)
//    {
//        var sanitizedExpression = expression.Replace(" ", string.Empty);
//        return Evaluate(sanitizedExpression);
//    }

//    private double Evaluate(string expression)
//    {
//        var values = new Stack<double>();
//        var operators = new Stack<char>();

//        for (var i = 0; i < expression.Length; i++)
//        {
//            var currentChar = expression[i];

//            if (char.IsDigit(currentChar) || currentChar == '.')
//            {
//                var number = string.Empty;

//                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
//                {
//                    number += expression[i];
//                    i++;
//                }

//                values.Push(double.Parse(number));
//                i--;
//            }
//            else if (currentChar == '(')
//            {
//                operators.Push(currentChar);
//            }
//            else if (currentChar == ')')
//            {
//                while (operators.Peek() != '(')
//                {
//                    values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
//                }
//                operators.Pop();
//            }
//            else if ("+-*/".Contains(currentChar))
//            {
//                while (operators.Count > 0 && HasPrecedence(currentChar, operators.Peek()))
//                {
//                    values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
//                }
//                operators.Push(currentChar);
//            }
//        }

//        while (operators.Count > 0)
//        {
//            values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
//        }

//        return values.Pop();
//    }

//    private bool HasPrecedence(char currentOperator, char stackOperator)
//    {
//        return currentOperator is '+' or '-' && stackOperator is '*' or '/';
//    }

//    private double ApplyOperator(char operatorChar, double b, double a)
//    {
//        return operatorChar switch
//        {
//            '+' => a + b,
//            '-' => a - b,
//            '*' => a * b,
//            '/' => a / b,
//            _ => throw new InvalidOperationException($"Unknown operator: {operatorChar}")
//        };
//    }

//    private Tuple<string, List<object>> ParseTransformation(string transformation)
//    {
//        var openParenIndex = transformation.IndexOf('(');
//        var closeParenIndex = transformation.IndexOf(')');

//        if (openParenIndex == -1 || closeParenIndex == -1)
//        {
//            return Tuple.Create(transformation, new List<object>());
//        }

//        var functionName = transformation[..openParenIndex].Trim();
//        var argumentsString = transformation.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
//        var arguments = ParseArguments(argumentsString);

//        return Tuple.Create(functionName, arguments);
//    }

//    private List<object> ParseArguments(string argumentsString)
//    {
//        var arguments = argumentsString.Split(',');
//        return arguments.Select(arg => arg.Trim()).Cast<object>().ToList();
//    }

//    private object ApplyTransformation(object? value, WorkflowFunction function, List<object> arguments)
//    {
//        function.ValidateArguments(arguments);
//        return function.Transform(value, arguments);
//    }

//    private int FindClosingBracket(string text, int startIndex)
//    {
//        var depth = 0;

//        for (var i = startIndex; i < text.Length; i++)
//        {
//            switch (text[i])
//            {
//                case '[':
//                    depth++;
//                    break;
//                case ']' when depth == 0:
//                    return i;
//                case ']':
//                    depth--;
//                    break;
//            }
//        }

//        return -1;
//    }
//}