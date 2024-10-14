using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.IO.Serialization;
using Newtonsoft.Json.Linq;
using System.Data;

namespace FlowSynx.Connectors.Stream.Json;

public class JsonHandler
{
    private readonly ISerializer _serializer;

    public string ContentType => "application/json";
    public string Extension => ".json";

    public JsonHandler(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public byte[] Load(string fullPath)
    {
        return File.ReadAllBytes(fullPath);
    }

    public string ToJson(DataTable dataTable, bool? indented)
    {
        string jsonString = string.Empty;

        if (dataTable != null && dataTable.Rows.Count > 0)
        {
            var config = new JsonSerializationConfiguration { Indented = indented ?? true };
            jsonString = _serializer.Serialize(dataTable, config);
        }

        return jsonString;
    }

    public string ToJson(DataRow datarow, bool? indented)
    {
        var dict = new Dictionary<string, object>();
        foreach (DataColumn col in datarow.Table.Columns)
        {
            dict.Add(col.ColumnName, datarow[col]);
        }

        var config = new JsonSerializationConfiguration { Indented = indented ?? true };
        return _serializer.Serialize(dict, config);
    }

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

    public IEnumerable<string> GetColumnNames(DataTable dataTable)
    {
        return dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName);
    }

    public IEnumerable<TransferDataRow> GenerateTransferDataRow(DataTable dataTable, bool? indented = false)
    {
        var transferDataRows = new List<TransferDataRow>();
        foreach (DataRow row in dataTable.Rows)
        {
            var itemArray = row.ItemArray;
            var rowContent = ToJson(row, indented);
            transferDataRows.Add(new TransferDataRow
            {
                Key = $"{Guid.NewGuid()}{Extension}",
                ContentType = ContentType,
                Content = rowContent.ToBase64String(),
                Items = itemArray
            });
        }

        return transferDataRows;
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