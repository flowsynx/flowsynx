using System.Data;
using FlowSynx.Connectors.Abstractions;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Connectors.Stream.Json.Services;

public interface IJsonManager
{
    string ContentType { get; }
    string Extension { get; }
    byte[] Load(string fullPath);
    string ToJson(DataTable dataTable, bool? indented);
    string ToJson(DataRow dataRow, bool? indented);
    IDictionary<string, object?> Flatten(JToken token, string prefix = "");
    IEnumerable<string> GetColumnNames(DataTable dataTable);
    void Delete(DataTable allRows, DataTable rowsToDelete);
    IEnumerable<TransferDataRow> GenerateTransferDataRow(DataTable dataTable, bool? indented = false);
}