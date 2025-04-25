using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParser : IExpressionParser
{
    private readonly Dictionary<string, object?> _outputs;

    public ExpressionParser(Dictionary<string, object?> outputs)
    {
        _outputs = outputs;
    }

    public object? Parse(string? expression)
    {
        if (expression == null)
            return null;

        const string pattern = @"\$\[Outputs\('([^']+)'\)(.*?)\]";
        var regex = new Regex(pattern);
        var matches = regex.Matches(expression);

        foreach (Match match in matches)
        {
            string outputName = match.Groups[1].Value;
            string accessPath = match.Groups[2].Value; // Includes dot properties and array indices

            if (!_outputs.ContainsKey(outputName))
            {
                string message = string.Format(Resources.ExpressionParser_OutputNotFound, outputName);
                throw new FlowSynxException((int)ErrorCode.ExpressionParserOutputNotFound, message);
            }

            var outputValue = _outputs[outputName];

            if (outputValue == null)
                return null;

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
        if (obj == null)
            return null;

        // Match properties or array indices: .Property, [index], .Nested[index], etc.
        const string pattern = @"(?:\.(\w+))|(?:\[(\d+)\])";
        var regex = new Regex(pattern);
        var matches = regex.Matches(accessPath);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                obj = GetPropertyValue(obj, match.Groups[1].Value);
            }
            else if (match.Groups[2].Success)
            {
                obj = GetArrayItem(obj, int.Parse(match.Groups[2].Value));
            }

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
        return propertyInfo?.GetValue(obj);
    }
}