﻿using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Database.MySql.Exceptions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using FlowSynx.Data.Sql;
using FlowSynx.Data.Extensions;
using FlowSynx.Data.Sql.Builder;
using System.Text;
using System.Threading;
using Org.BouncyCastle.Ocsp;
using FlowSynx.Data;

namespace FlowSynx.Connectors.Database.MySql.Services;

public class MysqlDatabaseManager : IMysqlDatabaseManager
{
    private readonly ILogger _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly ISqlBuilder _sqlBuilder;
    private readonly MySqlConnection _connection;
    private readonly Format _format;
    private string ContentType => "text/csv";
    private string Extension => ".csv";

    public MysqlDatabaseManager(ILogger logger, MySqlConnection connection,
        ISerializer serializer, IDeserializer deserializer, ISqlBuilder sqlBuilder)
    {
        _logger = logger;
        _connection = connection;
        _serializer = serializer;
        _deserializer = deserializer;
        _sqlBuilder = sqlBuilder;
        _format = Format.MySql;
    }

    public async Task Create(Context context, CancellationToken cancellationToken)
    {
        var createOptions = context.Options.ToObject<CreateOptions>();

        var createTableOption = GetCreateOption(createOptions);
        await CreateTable(createTableOption, cancellationToken);
    }

    public async Task Write(Context context, CancellationToken cancellationToken)
    {
        var writeFilters = context.Options.ToObject<WriteOptions>();

        var insertOption = GetInsertOption(writeFilters);
        var sql = _sqlBuilder.Insert(_format, insertOption);

        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Inserted {rowsAffected} row(s)!");
    }

    public async Task<InterchangeData> Read(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();

        var selectSqlOption = GetSelectOption(listOptions);
        var sql = _sqlBuilder.Select(_format, selectSqlOption);

        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable.Rows.Count switch
        {
            <= 0 => throw new DatabaseException("string.Format(Resources.NoItemsFoundWithTheGivenFilter)"),
            > 1 => throw new DatabaseException("Resources.FilteringDataMustReturnASingleItem"),
            _ => ReadData(ToString(dataTable, true))
        };
    }

    private InterchangeData ReadData(string content)
    {
        var result = new InterchangeData();
        result.Columns.Add("Content", typeof(byte[]));
        result.Rows.Add(content.ToByteArray());
        return result;
    }

    public Task Update(Context context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Delete(Context context, CancellationToken cancellationToken)
    {
        var deleteOptions = context.Options.ToObject<DeleteOptions>();

        var deleteOption = GetDeleteOption(deleteOptions);
        var sql = _sqlBuilder.Delete(_format, deleteOption);

        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Deleted {rowsAffected} row(s)!");

        if (deleteOptions.Purge is true)
            await Purge(deleteOptions.Table, cancellationToken);
    }

    public async Task<bool> Exist(Context context, CancellationToken cancellationToken)
    {
        var existOptions = context.Options.ToObject<ExistOptions>();

        var existSqlOption = GetExistRecordOption(existOptions);
        var sql = _sqlBuilder.ExistRecord(_format, existSqlOption);

        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.HasRows;
    }

    public async Task<InterchangeData> Entities(Context context, CancellationToken cancellationToken)
    {
        var dataTable = await FilteredEntities(context, cancellationToken);
        return dataTable;
    }

    public async Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken)
    {
        var listOptions = context.Options.ToObject<ListOptions>();

        var selectSqlOption = GetSelectOption(listOptions);
        var sql = _sqlBuilder.Select(_format, selectSqlOption);

        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dataTable = new InterchangeData();
        dataTable.Load(reader);
        
        return dataTable;
    }

    public Task Transfer(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken)
    //{
    //    var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);
    //    await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, transferKind, cancellationToken);
    //}

    //public async Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind,
    //    CancellationToken cancellationToken)
    //{
    //    var writeOptions = context.Options.ToObject<WriteOptions>();

    //    if (!TableExist(writeOptions.Table))
    //    {
    //        var tableFieldList = new CreateTableFieldList();
    //        tableFieldList.AddRange(transferData.Columns.Select(x => new CreateTableField
    //        {
    //            Name = x.Name,
    //            Type = _format.GetDbType(x.DataType)
    //        }));

    //        var tableCreateOption = new CreateOption
    //        {
    //            Table = writeOptions.Table,
    //            Fields = tableFieldList
    //        };

    //        await CreateTableAsync(tableCreateOption, cancellationToken);
    //        _logger.LogInformation($"Table {writeOptions.Table} was not exist, then created successfully!");
    //    }

    //    var columnNames = transferData.Columns.Select(x => x.Name).ToArray();
    //    var columns = string.Join(",", columnNames);
    //    var fields = new BulkInsertFieldsList { columns };

    //    var insertValueList = new BulkInsertValueList();
    //    foreach (var dr in transferData.Rows)
    //    {
    //        if (dr.Items == null) continue;
    //        var insertValue = new InsertValueList();
    //        insertValue.AddRange(dr.Items!);
    //        insertValueList.Add(insertValue);
    //    }

    //    var bulkInsertOptions = new BulkInsertOption { Table = writeOptions.Table, Fields = fields, Values = insertValueList };
    //    var bulkInsertSqlCommand = _sqlBuilder.BulkInsert(_format, bulkInsertOptions);

    //    var command = new MySqlCommand(bulkInsertSqlCommand, _connection);
    //    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
    //    _logger.LogInformation($"Deleted {rowsAffected} row(s)!");
    //}

    public async Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken)
    {
        var filteredData = await FilteredEntities(context, cancellationToken);

        if (filteredData.Rows.Count <= 0)
            throw new DatabaseException("string.Format(Resources.NoItemsFoundWithTheGivenFilter, path)");

        var compressOptions = context.Options.ToObject<CompressOptions>();

        if (compressOptions.SeparateDataPerRow is false)
            return await CompressDataTable(filteredData);

        return await CompressDataRows(filteredData.Rows);
    }

    #region internal methods
    //private async Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type,
    //Context context, CancellationToken cancellationToken = default)
    //{
    //    var transferOptions = context.Options.ToObject<TransferOptions>();

    //    var delimiter = GetDelimiter();
    //    var filteredData = await FilteredEntitiesAsync(context, cancellationToken);

    //    var transferDataRows = new List<TransferDataRow>();
    //    var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //    var isSeparateDataPerRow = transferOptions.SeparateDataPerRow is true;
    //    var csvContentBase64 = string.Empty;

    //    if (!isSeparateDataPerRow)
    //    {
    //        var csvContent = ToCsv(filteredData, delimiter);
    //        csvContentBase64 = csvContent.ToBase64String();
    //    }

    //    foreach (DataRow row in filteredData.Rows)
    //    {
    //        var itemArray = row.ItemArray;
    //        var content = isSeparateDataPerRow ? ToCsv(row, columnNames, delimiter) : ToCsv(row, delimiter);

    //        transferDataRows.Add(new TransferDataRow
    //        {
    //            Key = $"{GetPrimaryKeysValue(row)}{Extension}",
    //            ContentType = ContentType,
    //            Content = content.ToBase64String(),
    //            Items = itemArray
    //        });
    //    }

    //    return new TransferData
    //    {
    //        Namespace = @namespace,
    //        ConnectorType = type,
    //        ContentType = isSeparateDataPerRow ? string.Empty : ContentType,
    //        Content = isSeparateDataPerRow ? string.Empty : csvContentBase64,
    //        Columns = GetTransferDataColumn(filteredData),
    //        Rows = transferDataRows
    //    };
    //}

    private async Task Purge(string tableName, CancellationToken cancellationToken)
    {
        var selectSqlOption = GetDropTableOption(tableName);
        var sql = _sqlBuilder.DropTable(_format, selectSqlOption);
        var command = new MySqlCommand(sql, _connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Table dropped successfully!");
    }

    private string ToString(DataTable dataTable, bool? indented)
    {
        var jsonString = string.Empty;

        if (dataTable is { Rows.Count: > 0 })
        {
            var config = new JsonSerializationConfiguration { Indented = indented ?? true };
            jsonString = _serializer.Serialize(dataTable, config);
        }

        return jsonString;
    }

    private CreateOption GetCreateOption(CreateOptions options) => new()
    {
        Table = options.Table,
        Fields = GetCreateTableFields(options.Fields)
    };

    private SelectOption GetSelectOption(ListOptions options) => new()
    {
        Table = options.Table,
        Fields = GetFields(options.Fields),
        Join = GetJoinList(options.Join),
        Filter = GetFilterList(options.Filter),
        GroupBy = GetGroupBy(options.GroupBy),
        Sort = GetSortList(options.Sort),
        Paging = GetPaging(options.Paging)
    };

    private ExistRecordOption GetExistRecordOption(ExistOptions options) => new()
    {
        Table = options.Table,
        Filter = GetFilterList(options.Filter)
    };

    private InsertOption GetInsertOption(WriteOptions options) => new()
    {
        Table = options.Table,
        Fields = GetInsertFields(options.Fields),
        Values = GetInsertValueList(options.Values)
    };

    private DeleteOption GetDeleteOption(DeleteOptions options) => new()
    {
        Table = options.Table,
        Filter = GetFilterList(options.Filter)
    };

    private DropTableOption GetDropTableOption(string tableName) => new()
    {
        Table = tableName,
    };

    private CreateTableFieldList GetCreateTableFields(string? json)
    {
        var result = new CreateTableFieldList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<CreateTableFieldList>(json);
        }

        return result;
    }

    private FlowSynx.Data.Sql.FieldsList GetFields(string? json)
    {
        var result = new FlowSynx.Data.Sql.FieldsList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FlowSynx.Data.Sql.FieldsList>(json);
        }

        return result;
    }

    private InsertFieldsList GetInsertFields(string? json)
    {
        var result = new InsertFieldsList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<InsertFieldsList>(json);
        }

        return result;
    }

    private InsertValueList GetInsertValueList(string? json)
    {
        var result = new InsertValueList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<InsertValueList>(json);
        }

        return result;
    }

    private JoinList GetJoinList(string? json)
    {
        var result = new JoinList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<JoinList>(json);
        }

        return result;
    }

    private FlowSynx.Data.Sql.FilterList GetFilterList(string? json)
    {
        var result = new FlowSynx.Data.Sql.FilterList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FlowSynx.Data.Sql.FilterList>(json);
        }

        return result;
    }

    private GroupByList GetGroupBy(string? json)
    {
        var result = new GroupByList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<GroupByList>(json);
        }

        return result;
    }

    private FlowSynx.Data.Sql.SortList GetSortList(string? json)
    {
        var result = new FlowSynx.Data.Sql.SortList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FlowSynx.Data.Sql.SortList>(json);
        }

        return result;
    }

    private FlowSynx.Data.Sql.Paging GetPaging(string? json)
    {
        var result = new FlowSynx.Data.Sql.Paging();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FlowSynx.Data.Sql.Paging>(json);
        }

        return result;
    }

    private async Task CreateTable(CreateOption createOption, CancellationToken cancellationToken)
    {
        var sql = _sqlBuilder.Create(_format, createOption);
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Created {rowsAffected} row(s)!");
    }

    private string ToCsv(DataTable dataTable, string delimiter)
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

    private string ToCsv(DataRow row, string[] headers, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(string.Join(delimiter, headers));

        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));

        return stringBuilder.ToString();
    }

    private string ToCsv(DataRow row, string delimiter)
    {
        var stringBuilder = new StringBuilder();
        var fields = row.ItemArray.Select(field => field is null ? string.Empty : field.ToString());
        stringBuilder.AppendLine(string.Join(delimiter, fields));
        return stringBuilder.ToString();
    }

    //private IEnumerable<TransferDataColumn> GetTransferDataColumn(DataTable dataTable)
    //{
    //    return dataTable.Columns.Cast<DataColumn>()
    //        .Select(x => new TransferDataColumn { Name = x.ColumnName, DataType = x.DataType });
    //}

    private Task<IEnumerable<CompressEntry>> CompressDataTable(DataTable dataTable)
    {
        var compressEntries = new List<CompressEntry>();
        var delimiter = GetDelimiter();
        var rowContent = ToCsv(dataTable, delimiter);

        compressEntries.Add(new CompressEntry
        {
            Name = $"{dataTable.TableName}{Extension}",
            ContentType = ContentType,
            Content = rowContent.ToByteArray(),
        });

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }

    private Task<IEnumerable<CompressEntry>> CompressDataRows(DataRowCollection dataRows)
    {
        var compressEntries = new List<CompressEntry>();
        var delimiter = GetDelimiter();

        foreach (DataRow row in dataRows)
        {
            try
            {
                var rowContent = ToCsv(row, delimiter);
                compressEntries.Add(new CompressEntry
                {
                    Name = $"{GetPrimaryKeysValue(row)}{Extension}",
                    ContentType = ContentType,
                    Content = rowContent.ToByteArray(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return Task.FromResult<IEnumerable<CompressEntry>>(compressEntries);
    }


    private string GetPrimaryKeysValue(DataRow row)
    {
        var primaryKeys = row.Table.PrimaryKey;
        var keys = new List<string>();

        foreach (var key in primaryKeys)
        {
            var keyValue = row[key.ColumnName].ToString() ?? string.Empty;
            keys.Add(keyValue);
        }

        return string.Join('-', keys);
    }

    private bool TableExist(string tableName)
    {
        var selectSqlOption = new SelectOption { Table = tableName };
        var sql = _sqlBuilder.Select(_format, selectSqlOption);

        try
        {
            var command = new MySqlCommand(sql, _connection);
            command.ExecuteScalar();
            return true;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    private string GetDelimiter() => ",";
    #endregion
}