using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParser(Dictionary<string, object?> outputs) : IExpressionParser
{
    public object? Parse(string? expression)
    {
        if (expression == null)
            return null;

        const string pattern = @"\$\[Outputs\('([^']+)'\)(.*?)\]";
        var regex = new Regex(pattern);
        var matches = regex.Matches(expression);

        foreach (Match match in matches)
        {
            var outputName = match.Groups[1].Value;
            var accessPath = match.Groups[2].Value; // Includes dot properties and array indices

            if (!outputs.TryGetValue(outputName, out var outputValue))
            {
                var message = string.Format(Localization.Get("ExpressionParser_OutputNotFound", outputName));
                throw new FlowSynxException((int)ErrorCode.ExpressionParserOutputNotFound, message);
            }

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

    private static object? GetNestedValue(object? obj, string accessPath)
    {
        if (obj == null)
            return null;

        // Match properties or array indices: .Property, [index], .Nested[index], etc.
        const string pattern = @"(?:\.(\w+))|(?:\[(\d+)\])";
        var regex = new Regex(pattern);
        var matches = regex.Matches(accessPath);

        foreach (Match match in matches)
        {
            if (obj == null) return null;

            // If obj is a string that looks like JSON, parse it now
            if (obj is string str)
            {
                var token = TryParseJson(str);
                if (token != null)
                {
                    obj = token;
                }
            }

            if (match.Groups[1].Success)
            {
                obj = GetPropertyValue(obj, match.Groups[1].Value);
            }
            else if (match.Groups[2].Success)
            {
                obj = GetArrayItem(obj, int.Parse(match.Groups[2].Value));
            }
        }

        return UnwrapJToken(obj);
    }

    private static object? GetArrayItem(object obj, int index)
    {
        if (obj is IList list && index >= 0 && index < list.Count)
        {
            return list[index];
        }
        else if (obj is JArray jArr && index >= 0 && index < jArr.Count)
        {
            return jArr[index];
        }
        return null;
    }

    private static object? GetPropertyValue(object? obj, string propertyKey)
    {
        if (obj is JObject jObj)
        {
            return jObj.TryGetValue(propertyKey, StringComparison.OrdinalIgnoreCase, out var token)
                ? token
                : null;
        }
        else if (obj is JToken jToken)
        {
            return jToken[propertyKey];
        }
        else
        {
            var propertyInfo = obj?.GetType()
                .GetProperty(propertyKey, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo != null)
                return propertyInfo.GetValue(obj);
        }

        return null;
    }

    private static JToken? TryParseJson(string str)
    {
        try
        {
            return JToken.Parse(str);
        }
        catch
        {
            return null;
        }
    }

    private static object? UnwrapJToken(object? value)
    {
        return value switch
        {
            JValue jValue => jValue.Value, // unwrap primitive values
            _ => value
        };
    }
}