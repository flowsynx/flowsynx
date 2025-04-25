using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.Workflow;

public class PlaceholderReplacer : IPlaceholderReplacer
{
    public void ReplacePlaceholdersInParameters(Dictionary<string, object?> parameters, IExpressionParser parser)
    {
        foreach (var key in parameters.Keys.ToList())
        {
            parameters[key] = ProcessValue(parameters[key], parser);
        }
    }

    private object? ProcessValue(object? value, IExpressionParser parser)
    {
        return value switch
        {
            Dictionary<string, object?> nestedDict => ReplaceAndReturn(nestedDict, parser),
            List<string> stringList => ProcessStringList(stringList, parser),
            JObject jObject => ReplaceAndReturn(jObject, parser),
            JArray jArray => ReplaceAndReturn(jArray, parser),
            string strValue => ProcessString(strValue, parser),
            _ => value
        };
    }

    private Dictionary<string, object?> ReplaceAndReturn(Dictionary<string, object?> dict, IExpressionParser parser)
    {
        ReplacePlaceholdersInParameters(dict, parser);
        return dict;
    }

    private JObject ReplaceAndReturn(JObject jObject, IExpressionParser parser)
    {
        ReplacePlaceholderInJObject(jObject, parser);
        return jObject;
    }

    private JArray ReplaceAndReturn(JArray jArray, IExpressionParser parser)
    {
        ReplacePlaceholderInJArray(jArray, parser);
        return jArray;
    }

    private List<string> ProcessStringList(List<string> stringList, IExpressionParser parser)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = parser.Parse(stringList[i]) as string ?? stringList[i];
        }
        return stringList;
    }

    private object? ProcessString(string strValue, IExpressionParser parser)
    {
        if (IsJson(strValue))
        {
            var parsedJson = JsonConvert.DeserializeObject(strValue);
            ReplacePlaceholderInJson(parsedJson, parser);
            return JsonConvert.SerializeObject(parsedJson);
        }
        return parser.Parse(strValue);
    }

    private void ReplacePlaceholderInJObject(JObject jObject, IExpressionParser parser)
    {
        foreach (var prop in jObject.Properties())
        {
            prop.Value = prop.Value.Type switch
            {
                JTokenType.String => JToken.FromObject(parser.Parse(prop.Value.ToString()) ?? prop.Value),
                JTokenType.Object => ReplaceAndReturn((JObject)prop.Value, parser),
                JTokenType.Array => ReplaceAndReturn((JArray)prop.Value, parser),
                _ => prop.Value
            };
        }
    }

    private void ReplacePlaceholderInJArray(JArray jArray, IExpressionParser parser)
    {
        for (int i = 0; i < jArray.Count; i++)
        {
            jArray[i] = jArray[i].Type switch
            {
                JTokenType.String => JToken.FromObject(parser.Parse(jArray[i].ToString()) ?? jArray[i]),
                JTokenType.Object => ReplaceAndReturn((JObject)jArray[i], parser),
                JTokenType.Array => ReplaceAndReturn((JArray)jArray[i], parser),
                _ => jArray[i]
            };
        }
    }

    private void ReplacePlaceholderInJson(object? json, IExpressionParser parser)
    {
        switch (json)
        {
            case JObject jObject:
                ReplacePlaceholderInJObject(jObject, parser);
                break;
            case JArray jArray:
                ReplacePlaceholderInJArray(jArray, parser);
                break;
        }
    }

    private bool IsJson(string str)
    {
        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
    }
}