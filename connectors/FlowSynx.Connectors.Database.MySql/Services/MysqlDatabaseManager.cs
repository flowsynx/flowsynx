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
        var sql = sqlOptions.Sql ?? _sqlBuilder.Create(_format, createTableOption);
        
        var command = new MySqlCommand(sql, _connection);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Created {rowsAffected} row(s)!");
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
        var listOptions = context.Options.ToObject<ListOptions>();

        var selectSqlOption = GetSelectOption(listOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Select(_format, selectSqlOption);
        
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

    public Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
    #endregion
}