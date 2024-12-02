using FlowSynx.Connectors.Abstractions;
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
using System.Runtime.ConstrainedExecution;

namespace FlowSynx.Connectors.Database.MySql.Services;

public class MysqlDatabaseManager: IMysqlDatabaseManager
{
    private readonly ILogger _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly ISqlBuilder _sqlBuilder;
    private readonly MySqlConnection _connection;
    private readonly Format _format;

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

    public Task<object> About(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        throw new DatabaseException(Resources.AboutOperrationNotSupported);
    }

    public async Task CreateAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        var createTableOption = GetCreateOption(createOptions);
        await CreateTableAsync(createTableOption, cancellationToken);
    }

    public async Task WriteAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var writeFilters = context.Options.ToObject<WriteOptions>();

        var insertOption = GetInsertOption(writeFilters);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Insert(_format, insertOption);
        
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Inserted {rowsAffected} row(s)!");
    }

    public async Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        var selectSqlOption = GetSelectOption(listOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Select(_format, selectSqlOption);
        
        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable.Rows.Count switch
        {
            <= 0 => throw new DatabaseException("string.Format(Resources.NoItemsFoundWithTheGivenFilter)"),
            > 1 => throw new DatabaseException("Resources.FilteringDataMustReturnASingleItem"),
            _ => new ReadResult { Content = ToString(dataTable, true).ToByteArray() }
        };
    }

    public Task UpdateAsync(Context context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();

        var deleteOption = GetDeleteOption(deleteOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Delete(_format, deleteOption);
        
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Deleted {rowsAffected} row(s)!");

        if (deleteOptions.Purge is true)
            await PurgeAsync(deleteOptions.Table, cancellationToken);
    }

    public async Task<bool> ExistAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var existOptions = context.Options.ToObject<ExistOptions>();

        var existtSqlOption = GetExistRecordOption(existOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.ExistRecord(_format, existtSqlOption);
        
        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.HasRows;
    }

    public async Task<IEnumerable<object>> EntitiesAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        
        var selectSqlOption = GetSelectOption(listOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Select(_format, selectSqlOption);
        
        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable.DataTableToList();
    }

    public async Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        TransferKind transferKind, CancellationToken cancellationToken)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new DatabaseException(Resources.CalleeConnectorNotSupported);
        
        var transferData = await PrepareDataForTransferring(@namespace, type, sourceContext, cancellationToken);
        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, transferKind, cancellationToken);
    }

    public async Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind, 
        CancellationToken cancellationToken)
    {
        var createOptions = context.Options.ToObject<CreateOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        //var tableExistSql = _sqlBuilder.ExistTable(_format, new ExistTableOption { Table = writeOptions.Table });
        //var command = new MySqlCommand(tableExistSql, _connection);
        //var reader = await command.ExecuteReaderAsync(cancellationToken);
        //if (reader != null && reader.HasRows)
        //{
        //    var tableColumns = await GetColumnsNames(writeOptions.Table, cancellationToken);
        //    var transferColumns = transferData.Columns.Select(c => c.Name).AsEnumerable();
        //    bool allExist = transferColumns.All(item => tableColumns.Contains(item));

        //    if (!allExist)
        //        throw new DatabaseException("Table fields are not matched!");
        //}
        //else
        //{
            var tableFieldList = new CreateTableFieldList();
            tableFieldList.AddRange(transferData.Columns.Select(x => new CreateTableField { Name = x.Name, Type = _format.GetDbType(x.DataType) }));

            var tableCreateOption = new CreateOption
            {
                Table = writeOptions.Table,
                Fields = tableFieldList
            };

        await CreateTableAsync(tableCreateOption, cancellationToken);
        //}

        await BulkInsert(writeOptions.Table, transferData, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEnumerable<string>> GetColumnsNames(string tableName, CancellationToken cancellationToken)
    {
        List<string> listacolumnas = new List<string>();
        var tableExistSql = _sqlBuilder.TableFields(_format, new TableFieldsOption { Table = tableName });
        var command = new MySqlCommand(tableExistSql, _connection);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            reader.Read();

            var table = reader.GetSchemaTable();
            foreach (DataColumn column in table.Columns)
            {
                listacolumnas.Add(column.ColumnName);
            }
        }

        return listacolumnas;
    }

    private async Task BulkInsert(string tableName, TransferData transferData, CancellationToken cancellationToken)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            var columnNames = transferData.Columns.Select(x=>x.Name).ToArray();
            var columns = string.Join(",", columnNames);
            sb.Append($"INSERT INTO {tableName} ({columns}) VALUES ");

            if (transferData.Rows.Any())
            {
                foreach (var dr in transferData.Rows)
                {
                    if (dr != null && dr.Items != null)
                    {
                        var row = string.Join(",", dr.Items.Select(GetValue));
                        sb.Append($"({row}),");
                    }
                }

                var command = new MySqlCommand(sb.ToString(), _connection);
                var reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            else
            {
                throw new Exception("No row for insertion");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Please attach file in Proper format.");
        }
    }

    public Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    #region internal methods
    private Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type,
    Context context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task PurgeAsync(string tableName, CancellationToken cancellationToken)
    {
        var selectSqlOption = GetDropTableOption(tableName);
        var sql = _sqlBuilder.DropTable(_format, selectSqlOption);
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Drop table successfully!");
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
        Fields = GetFields(options.Fields),
        Values = GetValueList(options.Values)
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

    private FieldsList GetFields(string? json)
    {
        var result = new FieldsList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FieldsList>(json);
        }

        return result;
    }

    private ValueList GetValueList(string? json)
    {
        var result = new ValueList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<ValueList>(json);
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

    private FilterList GetFilterList(string? json)
    {
        var result = new FilterList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<FilterList>(json);
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

    private SortList GetSortList(string? json)
    {
        var result = new SortList();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<SortList>(json);
        }

        return result;
    }

    private Paging GetPaging(string? json)
    {
        var result = new Paging();
        if (!string.IsNullOrEmpty(json))
        {
            result = _deserializer.Deserialize<Paging>(json);
        }

        return result;
    }

    private async Task CreateTableAsync(CreateOption createOption, CancellationToken cancellationToken)
    {
        var sql = _sqlBuilder.Create(_format, createOption);
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Created {rowsAffected} row(s)!");
    }

    private object? GetValue(object? obj)
    {
        if (obj?.GetType() == typeof(string))
            return $"'{obj.ToString()}'";

        return obj;
    }
    #endregion
}