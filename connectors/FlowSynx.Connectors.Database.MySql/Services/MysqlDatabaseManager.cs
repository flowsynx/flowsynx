using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Database.MySql.Exceptions;
using FlowSynx.Connectors.Database.MySql.Extensions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.Data.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;

namespace FlowSynx.Connectors.Database.MySql.Services;

public class MysqlDatabaseManager: IMysqlDatabaseManager
{
    private readonly ILogger _logger;
    private readonly MySqlConnection _connection;

    public MysqlDatabaseManager(ILogger logger, MySqlConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }

    public async Task CreateAsync(string sql, CreateOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

        if (sql.IsCreateDatabaseStatement())
            throw new DatabaseException("Create database statement is not allowed!");

        var command = new MySqlCommand(sql, _connection);
        int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation($"Created {rowsAffected} row(s)!");
    }

    public async Task WriteAsync(string sql, WriteOptions options, object dataOptions, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

        var command = new MySqlCommand(sql, _connection);
        int rowsAffected = await command.ExecuteNonQueryAsync();
        _logger.LogInformation($"Inserted {rowsAffected} row(s)!");
    }

    public Task<ReadResult> ReadAsync(string sql, ReadOptions options, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string sql, DeleteOptions options, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task PurgeAsync(string sql, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistAsync(string sql, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public async Task<IEnumerable<object>> EntitiesAsync(string sql, QueryOptions queryOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("Resources.TheSpecifiedPathMustBeNotEmpty");

        var command = new MySqlCommand(sql, _connection);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable.CreateListFromTable();
    }

    public Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string sql, QueryOptions queryOptions,
        ReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}