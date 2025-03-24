using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Workflow;

public class ExpressionParser
{
    private Dictionary<string, object?> _outputs;

    public ExpressionParser(Dictionary<string, object?> outputs)
    {
        _outputs = outputs;
    }

    public object? Parse(string? expression)
    {
        if (expression == null)
            return expression;

        string pattern = @"\$\[Outputs\('([^']+)'\)(.*?)\]";
        Regex regex = new Regex(pattern);
        var matches = regex.Matches(expression);

        foreach (Match match in matches)
        {
            string outputName = match.Groups[1].Value;
            string accessPath = match.Groups[2].Value; // Includes dot properties and array indices

            if (!_outputs.ContainsKey(outputName))
                throw new ArgumentException($"Output '{outputName}' not found.");

            var outputValue = _outputs[outputName];

            if (outputValue is null)
                return outputValue;

            if (!string.IsNullOrEmpty(accessPath))
            {
                outputValue = GetNestedValue(outputValue, accessPath);
            }

            return outputValue;
        }

        return expression;
    }

    private object? GetNestedValue(object? obj, string accessPath)
    {
        if (obj is null) 
            return obj;

        // Match properties or array indices: .Property, [index], .Nested[index], etc.
        string pattern = @"(?:\.(\w+))|(?:\[(\d+)\])";
        Regex regex = new Regex(pattern);
        MatchCollection matches = regex.Matches(accessPath);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success) // Property access: .PropertyName
                obj = GetPropertyValue(obj, match.Groups[1].Value);
            else if (match.Groups[2].Success) // Array index access: [index]
                obj = GetArrayItem(obj, int.Parse(match.Groups[2].Value));
            
            if (obj == null)
                return null;
        }

        return obj;
    }

    private object? GetArrayItem(object obj, int index)
    {
        if (obj is IList list && index >= 0 && index < list.Count)
        {
            return list[index];
        }
        return null;
    }

    private object? GetPropertyValue(object obj, string propertyKey)
    {
        var propertyInfo = obj?.GetType().GetProperty(propertyKey, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (propertyInfo != null)
        {
            return propertyInfo.GetValue(obj);
        }
        return null;
    }
}