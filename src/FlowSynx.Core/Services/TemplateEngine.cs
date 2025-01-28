using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace FlowSynx.Core.Services;

public class TemplateEngine
{
    private readonly JObject _variables;
    private readonly IDictionary<string, TransformationFunction> _functions;

    public TemplateEngine(JObject variables)
    {
        _variables = variables;
        _functions = new Dictionary<string, TransformationFunction>();
    }

    public void RegisterTransformation(string name, TransformationFunction transformation)
    {
        _functions[name] = transformation;
    }

    public string Render(string template)
    {
        var result = new StringBuilder();
        var cursor = 0;

        while (cursor < template.Length)
        {
            var startIndex = template.IndexOf("$[", cursor);
            if (startIndex == -1)
            {
                result.Append(template.Substring(cursor));
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
        //object value = IsVariableReference(valueExpression) ? ResolveVariable(valueExpression) : ResolveOrEvaluate(valueExpression);
        object value;

        if (valueExpression.Contains("(") && valueExpression.Contains(")") && !IsVariableReference(valueExpression))
        {
            var transformationParts = ParseTransformation(valueExpression);
            var transformationName = transformationParts.Item1;
            var arguments = transformationParts.Item2;

            // Recursively resolve arguments of the transformation
            for (int i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is string argString)
                {
                    arguments[i] = EvaluateExpression(argString); // Recursively evaluate nested expressions
                }
            }

            if (_functions.TryGetValue(transformationName, out var func))
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
            var transformationParts = ParseTransformation(trimmedTransformation);
            var transformationName = transformationParts.Item1;
            var arguments = transformationParts.Item2;

            // Recursively resolve arguments of the transformation
            for (int i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is string argString)
                {
                    arguments[i] = EvaluateExpression(argString); // Recursively evaluate nested expressions
                }
            }

            if (_functions.TryGetValue(transformationName, out var func))
            {
                value = ApplyTransformation(value, func, arguments);
            }
            else
            {
                throw new Exception($"Unknown transformation: {transformationName}");
            }
        }

        return value.ToString();
    }

    private bool IsVariableReference(string expression)
    {
        return expression.StartsWith("variables(");
    }

    private object ResolveVariable(string expression)
    {
        var varName = expression.Substring(10, expression.Length - 11);
        return ResolveVariablePath(varName);
    }

    private object ResolveVariablePath(string path)
    {
        var segments = path.Split('.');
        object current = _variables;

        foreach (var segment in segments)
        {
            var isArrayAccess = segment.Contains("[");
            var baseSegment = isArrayAccess ? segment.Substring(0, segment.IndexOf('[')) : segment;

            if (current is JObject jObject)
            {
                current = jObject[baseSegment];
            }
            else if (current is IDictionary<string, object> dict)
            {
                current = dict.TryGetValue(baseSegment, out var value) ? value : null;
            }
            else
            {
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

        return current;
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
            var startIndex = expression.IndexOf("variables(", cursor);
            if (startIndex == -1)
            {
                result.Append(expression.Substring(cursor));
                break;
            }

            result.Append(expression.Substring(cursor, startIndex - cursor));

            var endIndex = expression.IndexOf(")", startIndex);
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

        for (int i = 0; i < expression.Length; i++)
        {
            char currentChar = expression[i];

            if (char.IsDigit(currentChar) || currentChar == '.')
            {
                string number = string.Empty;

                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    number += expression[i];
                    i++;
                }

                values.Push(double.Parse(number));
                i--;
            }
            else if (currentChar == '(')
            {
                operators.Push(currentChar);
            }
            else if (currentChar == ')')
            {
                while (operators.Peek() != '(')
                {
                    values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                }
                operators.Pop();
            }
            else if ("+-*/".Contains(currentChar))
            {
                while (operators.Count > 0 && HasPrecedence(currentChar, operators.Peek()))
                {
                    values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                }
                operators.Push(currentChar);
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
        return (currentOperator == '+' || currentOperator == '-') && (stackOperator == '*' || stackOperator == '/');
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

        var functionName = transformation.Substring(0, openParenIndex).Trim();
        var argsString = transformation.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
        var args = ParseArguments(argsString);

        return Tuple.Create(functionName, args);
    }

    private List<object> ParseArguments(string argsString)
    {
        var arguments = new List<object>();
        var segments = argsString.Split(',');

        foreach (var segment in segments)
        {
            arguments.Add(segment.Trim());
        }

        return arguments;
    }

    private object ApplyTransformation(object value, TransformationFunction func, List<object> arguments)
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
}

public class TransformationFunction
{
    public string Name { get; }
    public int ExpectedArgumentCount { get; }
    public Func<List<object>, object> Apply { get; }

    public TransformationFunction(string name, int expectedArgumentCount, Func<List<object>, object> apply)
    {
        Name = name;
        ExpectedArgumentCount = expectedArgumentCount;
        Apply = apply;
    }
}

//public class TemplateEngine
//{
//    private readonly JObject _variables;
//    private readonly IDictionary<string, Func<List<object>, object>> _functions;
//    //    private static Dictionary<string, Func<JObject, List<object>, string>> _customFunctions = new();

//    public TemplateEngine(JObject variables)
//    {
//        _variables = variables;
//        _functions = new Dictionary<string, Func<List<object>, object>>();
//    }

//    public void RegisterTransformation(string name, Func<List<object>, object> transformation)
//    {
//        _functions[name] = transformation;
//    }

//    /// <summary>
//    /// Render the JSON template by replacing placeholders with resolved values.
//    /// </summary>
//    public string Render(string template)
//    {
//        var result = new StringBuilder();
//        var cursor = 0;

//        while (cursor < template.Length)
//        {
//            var startIndex = template.IndexOf("$[", cursor);
//            if (startIndex == -1)
//            {
//                result.Append(template.Substring(cursor));
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

//    /// <summary>
//    /// Evaluates an expression, resolves variables, and applies transformations or math operations.
//    /// </summary>
//    private string EvaluateExpression(string expression)
//    {
//        var parts = expression.Split('|');
//        var valueExpression = parts[0].Trim();
//        var transformations = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

//        // Resolve the base value (including variables and math expressions)
//        var value = ResolveOrEvaluate(valueExpression);

//        // Apply each transformation in sequence, using the current value as input
//        foreach (var transformation in transformations)
//        {
//            var trimmedTransformation = transformation.Trim();
//            var transformationParts = ParseTransformation(trimmedTransformation);
//            var transformationName = transformationParts.Item1;
//            var arguments = transformationParts.Item2;

//            if (_functions.TryGetValue(transformationName, out var func))
//            {
//                // Pass the current value as the first argument
//                arguments.Insert(0, value);  // Add the current value as the first argument for the transformation
//                value = func(arguments);
//            }
//            else
//            {
//                throw new Exception($"Unknown transformation: {transformationName}");
//            }
//        }

//        return value.ToString();
//    }

//    /// <summary>
//    /// Parses the transformation string to separate the function name and its arguments.
//    /// </summary>
//    private Tuple<string, List<object>> ParseTransformation(string transformation)
//    {
//        var openParenIndex = transformation.IndexOf('(');
//        var closeParenIndex = transformation.IndexOf(')');

//        if (openParenIndex == -1 || closeParenIndex == -1)
//        {
//            return Tuple.Create(transformation, new List<object>());
//        }

//        var functionName = transformation.Substring(0, openParenIndex).Trim();
//        var argsString = transformation.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
//        var args = ParseArguments(argsString);

//        return Tuple.Create(functionName, args);
//    }

//    /// <summary>
//    /// Parses the arguments of a transformation function.
//    /// </summary>
//    private List<object> ParseArguments(string argsString)
//    {
//        var arguments = new List<object>();
//        var segments = argsString.Split(',');

//        foreach (var segment in segments)
//        {
//            var trimmed = segment.Trim();
//            if (trimmed.StartsWith("variables(") && trimmed.EndsWith(")"))
//            {
//                // Resolving variables as arguments
//                var varName = trimmed.Substring(10, trimmed.Length - 11);
//                var resolvedValue = ResolveVariable(varName);
//                arguments.Add(resolvedValue);
//            }
//            else if (double.TryParse(trimmed, out var numValue))
//            {
//                arguments.Add(numValue); // Parse numeric values directly
//            }
//            else
//            {
//                arguments.Add(trimmed); // Add as string
//            }
//        }

//        return arguments;
//    }

//    /// <summary>
//    /// Resolves variables in an expression and evaluates any math present.
//    /// </summary>
//    private object ResolveOrEvaluate(string expression)
//    {
//        // First resolve any variables (e.g., variables(user.age))
//        var resolvedExpression = ReplaceVariables(expression);

//        // If the resolved expression is purely numeric or contains valid math operators, evaluate it
//        if (IsMathExpression(resolvedExpression))
//        {
//            return EvaluateMathExpression(resolvedExpression);
//        }

//        return resolvedExpression; // Return the resolved expression as is
//    }

//    /// <summary>
//    /// Replaces variable paths (e.g., variables(user.age)) with their resolved values.
//    /// </summary>
//    private string ReplaceVariables(string expression)
//    {
//        var result = new StringBuilder();
//        var cursor = 0;

//        while (cursor < expression.Length)
//        {
//            var startIndex = expression.IndexOf("variables(", cursor);
//            if (startIndex == -1)
//            {
//                result.Append(expression.Substring(cursor));
//                break;
//            }

//            result.Append(expression.Substring(cursor, startIndex - cursor));

//            var endIndex = expression.IndexOf(")", startIndex);
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

//    /// <summary>
//    /// Resolves a variable path using dot notation or array indexing.
//    /// </summary>
//    private object ResolveVariable(string path)
//    {
//        var segments = path.Split('.');
//        object current = _variables;

//        foreach (var segment in segments)
//        {
//            var isArrayAccess = segment.EndsWith("]") && segment.Contains("[");
//            var baseSegment = isArrayAccess ? segment.Substring(0, segment.IndexOf('[')) : segment;

//            // Navigate object properties
//            if (current is JObject jObject)
//            {
//                current = jObject[baseSegment];
//            }
//            else if (current is IDictionary<string, object> dict)
//            {
//                current = dict.TryGetValue(baseSegment, out var value) ? value : null;
//            }
//            else
//            {
//                return string.Empty;
//            }

//            // Handle array access
//            if (isArrayAccess)
//            {
//                var indexString = segment.Substring(segment.IndexOf('[') + 1, segment.Length - baseSegment.Length - 2);
//                if (int.TryParse(indexString, out var index) && current is JArray jArray)
//                {
//                    current = index >= 0 && index < jArray.Count ? jArray[index] : null;
//                }
//                else
//                {
//                    return string.Empty;
//                }
//            }
//        }

//        return current;
//    }

//    /// <summary>
//    /// Determines if the expression is a valid math expression.
//    /// </summary>
//    private bool IsMathExpression(string expression)
//    {
//        // A basic check to identify if the expression looks like a math expression
//        return expression.Contains("+") || expression.Contains("-") || expression.Contains("*") || expression.Contains("/");
//    }

//    /// <summary>
//    /// Evaluates a math expression without using DataTable.Compute.
//    /// </summary>
//    private double EvaluateMathExpression(string expression)
//    {
//        var sanitizedExpression = SanitizeExpression(expression);
//        return Evaluate(sanitizedExpression);
//    }

//    /// <summary>
//    /// Helper function to sanitize and validate the expression.
//    /// </summary>
//    private string SanitizeExpression(string expression)
//    {
//        return expression.Replace(" ", string.Empty); // Remove all spaces
//    }

//    /// <summary>
//    /// Custom method to evaluate basic math expressions (supports +, -, *, /).
//    /// </summary>
//    private double Evaluate(string expression)
//    {
//        var values = new Stack<double>();
//        var operators = new Stack<char>();

//        for (int i = 0; i < expression.Length; i++)
//        {
//            char currentChar = expression[i];

//            if (char.IsDigit(currentChar) || currentChar == '.')
//            {
//                string number = string.Empty;

//                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
//                {
//                    number += expression[i];
//                    i++;
//                }

//                values.Push(double.Parse(number));
//                i--; // Adjust index
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

//    /// <summary>
//    /// Determines if the current operator has precedence over the one at the top of the stack.
//    /// </summary>
//    private bool HasPrecedence(char currentOperator, char stackOperator)
//    {
//        return (currentOperator == '+' || currentOperator == '-') && (stackOperator == '*' || stackOperator == '/');
//    }

//    /// <summary>
//    /// Applies the operator to two values.
//    /// </summary>
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

//    /// <summary>
//    /// Finds the matching closing bracket for a placeholder.
//    /// </summary>
//    private int FindClosingBracket(string input, int startIndex)
//    {
//        var depth = 1;
//        for (var i = startIndex; i < input.Length; i++)
//        {
//            if (input[i] == '[') depth++;
//            if (input[i] == ']') depth--;

//            if (depth == 0) return i;
//        }

//        return -1; // No matching closing bracket found
//    }

//    private string RenderValue(object value)
//    {
//        if (value is string)
//        {
//            return value.ToString();
//        }
//        else if (value is bool)
//        {
//            return value.ToString().ToLower();
//        }
//        else
//        {
//            return JsonConvert.SerializeObject(value);
//        }
//    }
//}











//public class TemplateEngine
//{
//    private readonly JObject _data;
//    private readonly Dictionary<string, Func<string, string>> transformations = new Dictionary<string, Func<string, string>>();

//    public TemplateEngine(JObject data)
//    {
//        _data = data;
//    }

//    public void RegisterTransformation(string name, Func<string, string> transformation)
//    {
//        transformations[name] = transformation;
//    }

//    // Function to process the template and replace placeholders
//    public string Render(string template)
//    {
//        var pattern = @"\$\[(.+?)\]"; // Pattern for finding placeholders in the template
//        var matches = Regex.Matches(template, pattern);

//        foreach (Match match in matches)
//        {
//            var placeholder = match.Groups[1].Value; // Extract placeholder content
//            var value = GetValueFromPlaceholder(placeholder, _data); // Get the value by evaluating the placeholder
//            template = template.Replace(match.Value, value); // Replace the placeholder with the value
//        }

//        return template;
//    }

//    // Function to get the value by evaluating the placeholder expression
//    private string GetValueFromPlaceholder(string placeholder, JObject data)
//    {
//        var parts = placeholder.Split('|');
//        var expression = parts[0].Trim(); // The base expression (e.g., variables(element.value))
//        var functions = parts.Skip(1).ToList(); // Transformation functions (e.g., function1, function2)

//        var value = EvaluateExpression(expression, data); // Evaluate the expression (can handle dynamic access)

//        foreach (var function in functions)
//        {
//            value = ApplyFunction(function.Trim(), value); // Apply transformations
//        }

//        return value;
//    }

//    // Function to evaluate dynamic expressions like variables(element.value) or variables(element[0])
//    private string EvaluateExpression(string expression, JObject data)
//    {
//        // Handle accessing data using expressions like variables(element.value) or variables(element[0])
//        var pattern = @"variables\((.*?)\)";
//        var match = Regex.Match(expression, pattern);

//        if (match.Success)
//        {
//            var fieldName = match.Groups[1].Value.Trim();

//            var tokens = fieldName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
//            JToken currentToken = data;

//            foreach (var token in tokens)
//            {
//                if (token.Contains('[') && token.Contains(']'))
//                {
//                    // Handling array access (e.g., element[0])
//                    var arrayToken = Regex.Match(token, @"([a-zA-Z0-9_]+)\[(\d+)\]");
//                    if (arrayToken.Success)
//                    {
//                        var arrayName = arrayToken.Groups[1].Value;
//                        var index = int.Parse(arrayToken.Groups[2].Value);
//                        currentToken = currentToken[arrayName]?[index];
//                    }
//                }
//                else
//                {
//                    currentToken = currentToken[token];
//                }

//                if (currentToken == null)
//                    return null;
//            }

//            return currentToken.ToString();




//            //var fieldName = match.Groups[1].Value.Trim();
//            //var parts = fieldName.Split(new[] { '.' }, StringSplitOptions.None);

//            //var currentData = data;
//            //foreach (var part in parts)
//            //{
//            //    var x = currentData[part];
//            //    currentData = currentData[part] as JObject;
//            //    if (currentData == null)
//            //        return string.Empty;
//            //}

//            //return currentData.ToString();
//        }

//        // Handle arrays with expression like element[0]
//        //if (expression.Contains("["))
//        //{
//        //    var arrayPattern = @"element\[(\d+)\]";
//        //    var arrayMatch = System.Text.RegularExpressions.Regex.Match(expression, arrayPattern);

//        //    if (arrayMatch.Success)
//        //    {
//        //        var index = int.Parse(arrayMatch.Groups[1].Value);
//        //        var arrayElement = data["element"][index];

//        //        return arrayElement?.ToString();
//        //    }
//        //}

//        return string.Empty; // Return empty string if no matching expression found
//    }

//    // Function to apply transformations (e.g., function1, function2)
//    private string ApplyFunction(string functionName, string value)
//    {
//        if (!transformations.ContainsKey(functionName))
//            return value;

//        return transformations[functionName](value);
//    }

//    // Custom transformation function 1
//    private string Function1(string value)
//    {
//        return value.ToUpper(); // Example transformation
//    }

//    // Custom transformation function 2
//    private string Function2(string value)
//    {
//        return value.ToLower(); // Example transformation
//    }
//}




//public class TemplateEngine
//{
//    private JObject _context;

//    // Constructor to load the JSON data
//    public TemplateEngine(JObject context)
//    {
//        _context = context;
//    }

//    private readonly Regex PlaceholderRegex = new Regex(@"\$\[(.*?)\]", RegexOptions.Compiled);

//    // Main method to process the JSON string with the provided context
//    public string Render(string jsonTemplate)
//    {
//        return PlaceholderRegex.Replace(jsonTemplate, match =>
//        {
//            string placeholder = match.Groups[1].Value;
//            return ResolvePlaceholder(placeholder, _context);
//        });
//    }

//    private string ResolvePlaceholder(string placeholder, JObject context)
//    {
//        // Separate placeholder and transformation (e.g., $[value | uppercase])
//        var parts = placeholder.Split('|').Select(p => p.Trim()).ToList();
//        string path = parts[0];

//        // Resolve the path in the context
//        var value = GetValueFromContext(path, context);

//        // Apply transformations
//        foreach (var transformation in parts.Skip(1))
//        {
//            value = ApplyTransformation(value, transformation);
//        }

//        return value.ToString();
//    }

//    // Get value from the context (supports nested paths and arrays)
//    private JToken GetValueFromContext(string path, JObject context)
//    {
//        string[] segments = path.Split(new[] { '[' }, StringSplitOptions.RemoveEmptyEntries);
//        JToken token = context;

//        foreach (var segment in segments)
//        {
//            string seg = segment;
//            if (segment.Contains(']'))
//                seg = segment.Substring(0, segment.IndexOf(']'));

//            if (int.TryParse(seg, out var index))
//            {
//                // It's an array
//                token = token[index];
//            }
//            else
//            {
//                // It's an object
//                token = token[seg];
//            }
//        }

//        return token;
//    }

//    // Apply transformations (e.g., uppercase, lowercase)
//    private JToken ApplyTransformation(JToken value, string transformation)
//    {
//        switch (transformation.ToLower())
//        {
//            case "uppercase":
//                return value.ToString().ToUpper();
//            case "lowercase":
//                return value.ToString().ToLower();
//            default:
//                throw new InvalidOperationException($"Unknown transformation: {transformation}");
//        }
//    }
//}






//public class TemplateEngine
//{
//    private static readonly Regex PlaceholderRegex = new Regex(@"\$\[(.*?)\]", RegexOptions.Compiled);
//    private readonly Dictionary<string, Func<string, string>> _functions;
//    private readonly Dictionary<string, string> _variables;

//    public TemplateEngine()
//    {
//        _functions = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
//        {
//            { "uppercase", value => value.ToUpper() },
//            { "lowercase", value => value.ToLower() },
//            { "reverse", value => ReverseString(value) }
//        };

//        _variables = new Dictionary<string, string>();
//    }

//    public string Render(string template)
//    {
//        return PlaceholderRegex.Replace(template, match =>
//        {
//            string placeholder = match.Groups[1].Value;

//            // Check for function pipe
//            if (placeholder.Contains('|'))
//            {
//                var parts = placeholder.Split('|');
//                string variableKey = parts[0].Trim();
//                string functionName = parts[1].Trim();

//                if (_variables.TryGetValue(variableKey, out var value) && _functions.TryGetValue(functionName, out var func))
//                {
//                    return func(value);
//                }

//                return match.Value; // Return the original placeholder if no match
//            }

//            // Resolve nested placeholders or regular variables
//            if (placeholder.StartsWith("variables(") && placeholder.EndsWith(")"))
//            {
//                string nestedKey = placeholder.Substring(10, placeholder.Length - 11);
//                if (_variables.TryGetValue(nestedKey, out var value))
//                {
//                    return Render(value); // Recursive evaluation
//                }
//            }
//            else if (_variables.TryGetValue(placeholder, out var result))
//            {
//                return result;
//            }

//            return match.Value; // Return the original placeholder if no match
//        });
//    }

//    public void RegisterFunction(string name, Func<string, string> function)
//    {
//        _functions[name] = function;
//    }

//    public void RegisterVariable(string key, string value)
//    {
//        _variables[key] = value;
//    }

//    private static string ReverseString(string input)
//    {
//        char[] arr = input.ToCharArray();
//        Array.Reverse(arr);
//        return new string(arr);
//    }
//}

//public class TemplateEngine
//{
//    private readonly Dictionary<string, Func<object[], object>> _customFunctions = new();

//    // Register a custom function
//    public void RegisterFunction(string name, Func<object[], object> function)
//    {
//        _customFunctions[name] = function;
//    }

//    // Process the template with the provided context
//    public string Render(string template, JObject context)
//    {
//        return Regex.Replace(template, @"\$\[([^\]]+)\]", match =>
//        {
//            var expression = match.Groups[1].Value;
//            return EvaluateExpression(expression, context)?.ToString() ?? string.Empty;
//        });
//    }

//    // Evaluate a single expression
//    private object EvaluateExpression(string expression, JObject context)
//    {
//        // Handle custom functions (e.g., toUpper(user.name))
//        var functionMatch = Regex.Match(expression, @"(\w+)\((.*)\)");
//        if (functionMatch.Success)
//        {
//            var functionName = functionMatch.Groups[1].Value;
//            var args = functionMatch.Groups[2].Value.Split(',').Select(arg => EvaluateExpression(arg.Trim(), context)).ToArray();

//            if (_customFunctions.TryGetValue(functionName, out var function))
//            {
//                return function(args);
//            }

//            throw new Exception($"Unknown function: {functionName}");
//        }

//        // Handle nested object access (e.g., user.name or order.items[0])
//        var tokens = expression.Split('.');
//        JToken current = context;

//        foreach (var token in tokens)
//        {
//            if (token.Contains('[')) // Handle array access
//            {
//                var arrayMatch = Regex.Match(token, @"(\w+)\[(\d+)\]");
//                var arrayName = arrayMatch.Groups[1].Value;
//                var index = int.Parse(arrayMatch.Groups[2].Value);

//                current = current[arrayName]?[index];
//            }
//            else
//            {
//                current = current[token];
//            }

//            if (current == null)
//            {
//                return null; // Return null for missing tokens
//            }
//        }

//        return current?.ToObject<object>();
//    }
//}

//public class TemplateEngine
//{
//    private readonly string _startToken;
//    private readonly string _endToken;

//    string pattern => Regex.Escape(_startToken) + @"\s*(.*?)\s*" + Regex.Escape(_endToken);
//    //private Regex VariablePattern => new Regex(pattern, RegexOptions.Compiled);
//    //private static readonly Regex VariablePattern = new Regex(@"\$\[\s*(.*?)\s*\]", RegexOptions.Compiled);

//    private readonly Dictionary<string, object> _variables = new();
//    private static Dictionary<string, Func<JObject, List<object>, string>> _customFunctions = new();
//    private readonly Dictionary<string, object> _results = new();

//    public TemplateEngine(string startToken = "{{", string endToken = "}}")
//    {
//        _startToken = startToken;
//        _endToken = endToken;
//    }

//    public void RegisterVariable(string key, object value)
//    {
//        _variables[key] = value;
//    }

//    // Method to register custom functions
//    public void RegisterFunction(string functionName, Func<JObject, List<object>, string> function)
//    {
//        _customFunctions[functionName] = function;
//    }

//    public void RegisterResult(string key, object value)
//    {
//        _results[key] = value;
//    }

//    public string Render(string template, object data)
//    {
//        var jObject = JObject.Parse(JsonConvert.SerializeObject(data));
//        return Render(template, jObject);
//    }

//    public string Render(string template, JObject data)
//    {
//        return Regex.Replace(template, pattern, match =>
//        {
//            var expression = match.Groups[1].Value;
//            return EvaluateExpression(expression, data);
//        });
//    }

//    private string EvaluateExpression(string expression, JObject data)
//    {
//        try
//        {
//            // Handle simple math expressions, functions, and data binding
//            if (expression.Contains("+") || expression.Contains("-") || expression.Contains("*") || expression.Contains("/"))
//            {
//                return EvaluateMathExpression(expression, data).ToString();
//            }

//            // Handle function calls with multiple parameters
//            if (expression.Contains("(") && expression.Contains(")"))
//            {
//                return EvaluateFunction(expression, data);
//            }

//            //// Simple data binding (e.g. userName or userAddress.street)
//            //var value = RenderValue(data.SelectToken(expression));
//            //return value ?? $"$[{expression}]"; // Return original if not found

//            return expression; // Return original if not found
//        }
//        catch
//        {
//            return $"$[{expression}]"; // Return original if error
//        }
//    }

//    private double EvaluateMathExpression(string expression, JObject data)
//    {
//        expression = Regex.Replace(expression, pattern, match =>
//        {
//            var varExpression = match.Groups[1].Value;
//            return data.SelectToken(varExpression)?.ToString() ?? "0";
//        });

//        // You could replace this with a more advanced math parser
//        var result = new System.Data.DataTable().Compute(expression, null);
//        return Convert.ToDouble(result);
//    }

//    private string EvaluateFunction(string function, JObject data)
//    {
//        var functionName = function.Split('(')[0];
//        var argsString = function.Substring(function.IndexOf('(') + 1, function.LastIndexOf(')') - function.IndexOf('(') - 1);
//        var arguments = argsString.Split(',').Select(arg => arg.Trim()).ToList();

//        var evaluatedArgs = arguments.Select(arg => EvaluateExpression(arg, data)).ToList();

//        if (_customFunctions.ContainsKey(functionName))
//        {
//            var args = evaluatedArgs.ConvertAll(item => (object)item);
//            return _customFunctions[functionName](data, args);
//        }

//        // Handle built-in functions
//        if (functionName == "calculateAge")
//        {
//            var birthDate = DateTime.Parse(data.SelectToken(arguments[0])?.ToString() ?? "1/1/2000");
//            return (DateTime.Now.Year - birthDate.Year).ToString();
//        }

//        if (functionName == "formatDate")
//        {
//            var date = DateTime.Parse(evaluatedArgs[0]);
//            var format = evaluatedArgs.Count > 1 ? evaluatedArgs[1] : "yyyy-MM-dd";
//            return date.ToString(format);
//        }

//        if (functionName == "if")
//        {
//            var condition = bool.Parse(evaluatedArgs[0]);
//            return condition ? evaluatedArgs[1] : evaluatedArgs[2];
//        }

//        return $"$[{function}]"; // Return original if function not found
//    }

//    private string RenderValue(object value)
//    {
//        if (value is string)
//        {
//            return value.ToString();
//        }
//        else if (value is bool)
//        {
//            return value.ToString().ToLower();
//        }
//        else
//        {
//            return JsonConvert.SerializeObject(value);
//        }
//    }

//    private object[] ParseArguments(string expression)
//    {
//        var args = expression.Split('(')[1].Split(')')[0];
//        return args.Split(',').Select(arg => (object)arg.Trim()).ToArray();
//    }
//}

//public class TemplateEngine
//{
//    private Dictionary<string, Func<object[], object>> _functions = new Dictionary<string, Func<object[], object>>();

//    public TemplateEngine()
//    {
//        // Add default functions
//        RegisterFunction("sum", (args) => args.Length == 2 ? (double)args[0] + (double)args[1] : 0);
//        RegisterFunction("multiply", (args) => args.Length == 2 ? (double)args[0] * (double)args[1] : 0);
//        RegisterFunction("concat", (args) => string.Concat(args.Select(arg => arg.ToString())));
//    }

//    public void RegisterFunction(string name, Func<object[], object> function)
//    {
//        _functions[name] = function;
//    }

//    public string Render(string template, Dictionary<string, object> data)
//    {
//        var regex = new Regex(@"\$\[([^]]+)\]");
//        var matches = regex.Matches(template);

//        foreach (Match match in matches)
//        {
//            var expression = match.Groups[1].Value.Trim();

//            // Check if the expression is a function call
//            if (expression.Contains("("))
//            {
//                var funcName = expression.Split('(')[0];
//                var args = ParseArguments(expression);
//                if (_functions.ContainsKey(funcName))
//                {
//                    var result = _functions[funcName](args);
//                    template = template.Replace(match.Value, result.ToString());
//                }
//            }
//            // Check for basic math or data binding
//            else if (expression.Contains("+") || expression.Contains("-") || expression.Contains("*") || expression.Contains("/"))
//            {
//                var result = EvaluateMathExpression(expression);
//                template = template.Replace(match.Value, result.ToString());
//            }
//            else
//            {
//                var result = GetDataBindingValue(data, expression);
//                template = template.Replace(match.Value, RenderValue(result));
//            }
//        }

//        return template;
//    }

//    private object[] ParseArguments(string expression)
//    {
//        var args = expression.Split('(')[1].Split(')')[0];
//        return args.Split(',').Select(arg => (object)arg.Trim()).ToArray();
//    }

//    private object EvaluateMathExpression(string expression)
//    {
//        var dataTable = new System.Data.DataTable();
//        return dataTable.Compute(expression, "");
//    }

//    private object GetDataBindingValue(Dictionary<string, object> data, string expression)
//    {
//        string[] keys = expression.Split('.');
//        object currentValue = data;

//        foreach (var key in keys)
//        {
//            var dictionary = currentValue as IDictionary<string, object>;
//            if (dictionary != null && dictionary.ContainsKey(key))
//            {
//                currentValue = dictionary[key];
//            }
//            else
//            {
//                return ""; // Return empty if key not found
//            }
//        }

//        return currentValue;
//    }

//    private string RenderValue(object value)
//    {
//        if (value is string)
//        {
//            return value.ToString();
//        }
//        else if (value is bool)
//        {
//            return value.ToString().ToLower();
//        }
//        else
//        {
//            return JsonConvert.SerializeObject(value);
//        }
//    }
//}