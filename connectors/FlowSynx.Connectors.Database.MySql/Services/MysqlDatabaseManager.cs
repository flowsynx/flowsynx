using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Database.MySql.Exceptions;
using FlowSynx.Connectors.Database.MySql.Extensions;
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
        throw new NotImplementedException();
    }

    public async Task CreateAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        var sql = sqlOptions.Sql;

        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

        if (sql.IsCreateDatabaseStatement())
            throw new DatabaseException("Create database statement is not allowed!");

        var command = new MySqlCommand(sql, _connection);
        int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Created {rowsAffected} row(s)!");
    }

    public async Task WriteAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var writeFilters = context.Options.ToObject<WriteOptions>();

        var sql = sqlOptions.Sql;

        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

        if (!sql.IsInsertStatement())
            throw new DatabaseException("The enterted sql statement is not valid Insert statement!");

        var command = new MySqlCommand(sql, _connection);
        int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Inserted {rowsAffected} row(s)!");
    }

    public async Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        var sql = sqlOptions.Sql;

        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

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

    public Task DeleteAsync(Context context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task PurgeAsync(Context context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistAsync(Context context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public async Task<IEnumerable<object>> EntitiesAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        
        var selectSqlOption = GetSelectOption(listOptions);
        var sql = sqlOptions.Sql ?? _sqlBuilder.Select(_format, selectSqlOption);

        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

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

    private SelectOption GetSelectOption(ListOptions options) => new()
    {
        Table = GetTable(options.Table),
        Fields = GetFields(options.Fields),
        Join = GetJoinList(options.Joins),
        Filter = GetFilterList(options.Filters),
        GroupBy = GetGroupBy(options.GroupBy),
        Sort = GetSortList(options.Sorts),
        Paging = GetPaging(options.Paging)
    };

    private Table GetTable(string json)
    {
        return _deserializer.Deserialize<Table>(json);
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