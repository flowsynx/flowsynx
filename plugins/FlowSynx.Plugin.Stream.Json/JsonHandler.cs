﻿using Newtonsoft.Json.Linq;
using System.Data;

namespace FlowSynx.Plugin.Stream.Json;

public class JsonHandler
{
    public IDictionary<string, object?> Flatten(JToken token, string prefix = "")
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
            var index = 1;
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

    public JObject UnFlattenJson(Dictionary<string, string> flatJson)
    {
        var result = new JObject();

        foreach (var kvp in flatJson)
        {
            AddToJObject(result, kvp.Key.Split('.'), kvp.Value);
        }

        return result;
    }

    private void AddToJObject(JObject current, IReadOnlyList<string> keys, string value)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            if (i == keys.Count - 1)
            {
                current[keys[i]] = value;
            }
            else
            {
                current[keys[i]] ??= new JObject();
                current = (JObject)current[keys[i]];
            }
        }
    }

    public void Delete(DataTable allRows, DataTable rowsToDelete)
    {
        foreach (DataRow rowToDelete in rowsToDelete.Rows)
        {
            foreach (DataRow row in allRows.Rows)
            {
                var rowToDeleteArray = rowToDelete.ItemArray;
                var rowArray = row.ItemArray;
                var equalRows = true;
                for (var i = 0; i < rowArray.Length; i++)
                {
                    if (!rowArray[i]!.Equals(rowToDeleteArray[i]))
                    {
                        equalRows = false;
                    }
                }

                if (!equalRows)
                    continue;

                allRows.Rows.Remove(row);
                break;
            }
        }
    }
}