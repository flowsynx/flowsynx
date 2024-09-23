using System.Data;
using System.Text;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;

namespace FlowSynx.Plugin.Stream.Csv;

internal class CsvHandler
{
    private readonly ILogger _logger;
    private readonly ISerializer _serializer;

    public CsvHandler(ILogger logger, ISerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public byte[] Load(string fullPath, string delimiter)
    {
        return File.ReadAllBytes(fullPath);
    }

    public DataTable Load(string fullPath, string delimiter, bool? includeMetaData)
    {
        var dataTable = new DataTable();
        using var streamReader = new StreamReader(fullPath);
        var firstLine = streamReader.ReadLine();
        var headers = firstLine?.Split(delimiter, StringSplitOptions.None);

        if (includeMetaData is true)
            headers = headers?.Append("Metadata").ToArray();
        
        if (headers != null)
        {
            foreach (var header in headers)
                dataTable.Columns.Add(header);

            var columnInterval = headers.Count();
            var newLine = streamReader.ReadLine();
            while (newLine != null)
            {
                object?[] fields = newLine.Split(delimiter, StringSplitOptions.None);
                if (includeMetaData is true)
                {
                    var metadataObject = GetMetaData(fields);
                    fields = fields.Append(metadataObject).ToArray();
                }

                var currentLength = fields.Count();
                if (currentLength < columnInterval)
                {
                    while (currentLength < columnInterval)
                    {
                        newLine += streamReader.ReadLine();
                        currentLength = newLine.Split(delimiter, StringSplitOptions.None).Count();
                    }

                    fields = newLine.Split(delimiter, StringSplitOptions.None);
                }

                if (currentLength > columnInterval)
                {
                    newLine = streamReader.ReadLine();
                    continue;
                }

                if (!fields.Any())
                    continue;

                dataTable.Rows.Add(fields);
                newLine = streamReader.ReadLine();
            }
        }

        streamReader.Close();
        return dataTable;
    }

    public string ToCsv(DataTable dataTable, string delimiter)
    {
        var stringBuilder = new StringBuilder();

        var columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        stringBuilder.AppendLine(string.Join(delimiter, columnNames));

        foreach (DataRow row in dataTable.Rows)
        {
            var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
            stringBuilder.AppendLine(string.Join(delimiter, fields));
        }

        return stringBuilder.ToString();
    }

    public string ToCsv(DataRow row, string[] headers, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(string.Join(delimiter, headers));

        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));
        
        return stringBuilder.ToString();
    }

    public string ToCsv(DataRow row, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));
        return stringBuilder.ToString();
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

    private object GetMetaData(object content)
    {
        var contentHash = FlowSynx.Security.HashHelper.Md5.GetHash(content);
        return new
        {
            ContentHash = contentHash
        };
    }
}