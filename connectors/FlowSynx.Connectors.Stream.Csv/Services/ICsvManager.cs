using System.Data;

namespace FlowSynx.Connectors.Stream.Csv.Services;

public interface ICsvManager
{
    string ContentType { get; }
    string Extension { get; }
    byte[] Load(string fullPath);
    DataTable Load(string content, string delimiter, bool? includeMetaData);
    string ToCsv(DataTable dataTable, string delimiter);
    string ToCsv(DataRow row, string[] headers, string delimiter);
    string ToCsv(DataRow row, string delimiter);
    void Delete(DataTable allRows, DataTable rowsToDelete);
}