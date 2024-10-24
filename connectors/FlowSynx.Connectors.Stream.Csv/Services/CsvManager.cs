﻿using System.Data;
using System.Text;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Connectors.Stream.Csv.Services;

internal class CsvManager: ICsvManager
{
    private readonly ILogger _logger;
    private readonly ISerializer _serializer;
    public string ContentType => "text/csv";
    public string Extension => ".csv";

    public CsvManager(ILogger logger, ISerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public byte[] Load(string fullPath)
    {
        return File.ReadAllBytes(fullPath);
    }

    public DataTable Load(string content, string delimiter, bool? includeMetaData)
    {
        var dataTable = new DataTable();
        var lines = content.Split(System.Environment.NewLine);

        if (lines.Length == 0)
            return dataTable;

        // Add columns
        var headers = lines[0].Split(delimiter, StringSplitOptions.None);

        if (includeMetaData is true)
            headers = headers.Append("Metadata").ToArray();

        foreach (var header in headers)
            dataTable.Columns.Add(header);

        var columnsCount = headers.Length;

        // Add rows
        for (var i = 1; i < lines.Length - 1; i++)
        {
            object[] fields = lines[i].Split(delimiter, StringSplitOptions.None);
            if (includeMetaData is true)
            {
                var metadataObject = GetMetaData(fields);
                fields = fields.Append(metadataObject).ToArray();
            }

            var currentLength = fields.Length;
            if (currentLength != columnsCount)
                continue;

            var dataRow = dataTable.NewRow();
            for (var j = 0; j < headers.Length; j++)
            {
                dataRow[j] = fields[j];
            }

            dataTable.Rows.Add(dataRow);
        }

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
        var contentHash = Security.HashHelper.Md5.GetHash(content);
        return contentHash;
    }
}