using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSynx.Plugin.Stream.Json;

public class JsonFlattener
{
    public static IDictionary<string, object?> Flatten(JToken token, string prefix = "")
    {
        var result = new Dictionary<string, object?>();

        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                var childPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}_{property.Name}";
                foreach (var child in Flatten(property.Value, childPrefix))
                {
                    result.Add(child.Key, child.Value);
                }
            }
        }

        else if (token.Type == JTokenType.Array)
        {
            var index = 0;
            foreach (var item in token.Children())
            {
                var childPrefix = $"{prefix}{index}";
                foreach (var child in Flatten(item, childPrefix))
                {
                    result.Add(child.Key, child.Value);
                }
                index++;
            }
        }
        else
        {
            result.Add(prefix, ((JValue)token).Value);
        }

        return result;
    }
}