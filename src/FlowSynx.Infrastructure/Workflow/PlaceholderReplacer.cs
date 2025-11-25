using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using FlowSynx.Infrastructure.Workflow.Expressions;

namespace FlowSynx.Infrastructure.Workflow;

public class PlaceholderReplacer : IPlaceholderReplacer
{
    public async Task<string> ReplacePlaceholders(string content, IExpressionParser expressionParser, CancellationToken cancellationToken = default)
    {
        var result = await ProcessString(content, expressionParser, cancellationToken);
        return result as string ?? content;
    }

    public async Task ReplacePlaceholdersInParameters(Dictionary<string, object?> parameters, IExpressionParser expressionParser, CancellationToken cancellationToken = default)
    {
        foreach (var key in parameters.Keys.ToList())
        {
            parameters[key] = await ProcessValue(parameters[key], expressionParser, cancellationToken);
        }
    }

    private async Task<object?> ProcessValue(object? value, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        return value switch
        {
            Dictionary<string, object?> nestedDict => await ReplaceAndReturn(nestedDict, expressionParser, cancellationToken),
            List<string> stringList => await ProcessStringList(stringList, expressionParser, cancellationToken),
            JObject jObject => await ReplaceAndReturn(jObject, expressionParser, cancellationToken),
            JArray jArray => await ReplaceAndReturn(jArray, expressionParser, cancellationToken),
            string strValue => await ProcessString(strValue, expressionParser, cancellationToken),
            _ => value
        };
    }

    private async Task<Dictionary<string, object?>> ReplaceAndReturn(Dictionary<string, object?> dict, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        await ReplacePlaceholdersInParameters(dict, expressionParser, cancellationToken);
        return dict;
    }

    private async Task<JObject> ReplaceAndReturn(JObject jObject, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        await ReplacePlaceholderInJObject(jObject, expressionParser, cancellationToken);
        return jObject;
    }

    private async Task<JArray> ReplaceAndReturn(JArray jArray, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        await ReplacePlaceholderInJArray(jArray, expressionParser, cancellationToken);
        return jArray;
    }

    private static async Task<List<string>> ProcessStringList(List<string> stringList, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        for (var i = 0; i < stringList.Count; i++)
        {
            var result = await expressionParser.ParseAsync(stringList[i], cancellationToken);
            stringList[i] = result as string ?? stringList[i];
        }
        return stringList;
    }

    private async Task<object?> ProcessString(string strValue, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (!IsJson(strValue))
            return await expressionParser.ParseAsync(strValue, cancellationToken);

        var parsedJson = JsonConvert.DeserializeObject(strValue);
        await ReplacePlaceholderInJson(parsedJson, expressionParser, cancellationToken);
        return JsonConvert.SerializeObject(parsedJson);
    }

    private async Task ReplacePlaceholderInJObject(JObject jObject, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        foreach (var prop in jObject.Properties().ToList())
        {
            prop.Value = prop.Value.Type switch
            {
                JTokenType.String => JToken.FromObject(await expressionParser.ParseAsync(prop.Value.ToString(), cancellationToken) ?? prop.Value),
                JTokenType.Object => await ReplaceAndReturn((JObject)prop.Value, expressionParser, cancellationToken),
                JTokenType.Array => await ReplaceAndReturn((JArray)prop.Value, expressionParser, cancellationToken),
                _ => prop.Value
            };
        }
    }

    private async Task ReplacePlaceholderInJArray(JArray jArray, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        for (var i = 0; i < jArray.Count; i++)
        {
            jArray[i] = jArray[i].Type switch
            {
                JTokenType.String => JToken.FromObject(await expressionParser.ParseAsync(jArray[i].ToString(), cancellationToken) ?? jArray[i]),
                JTokenType.Object => await ReplaceAndReturn((JObject)jArray[i], expressionParser, cancellationToken),
                JTokenType.Array => await ReplaceAndReturn((JArray)jArray[i], expressionParser, cancellationToken),
                _ => jArray[i]
            };
        }
    }

    private async Task ReplacePlaceholderInJson(object? json, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        switch (json)
        {
            case JObject jObject:
                await ReplacePlaceholderInJObject(jObject, expressionParser, cancellationToken);
                break;
            case JArray jArray:
                await ReplacePlaceholderInJArray(jArray, expressionParser, cancellationToken);
                break;
            default:
                // No action needed for other types
                break;
        }
    }

    private static bool IsJson(string str)
    {
        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
    }
}