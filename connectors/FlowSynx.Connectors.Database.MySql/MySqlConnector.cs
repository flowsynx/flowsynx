using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Database.MySql.Exceptions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.Connectors.Database.MySql.Services;
using FlowSynx.IO.Compression;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Connectors.Database.MySql;

public class MySqlConnector : Connector
{
    private readonly ILogger<MySqlConnector> _logger;
    private readonly IMySqlDatabaseConnection _connection;
    private IMysqlDatabaseManager _manager = null!;
    private MySqlpecifications _mysqlSpecifications = null!;

    public MySqlConnector(ILogger<MySqlConnector> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connection = new MySqlDatabaseConnection();
    }

    public override Guid Id => Guid.Parse("20fc1d0c-7fef-43e4-95bc-f99f3e5fd088");
    public override string Name => "MySql";
    public override Namespace Namespace => Namespace.Database;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(MySqlpecifications);

    public override Task Initialize()
    {
        _mysqlSpecifications = Specifications.ToObject<MySqlpecifications>();
        var connection = _connection.Connect(_mysqlSpecifications);
        _manager = new MysqlDatabaseManager(_logger, connection);
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();
        await _manager.CreateAsync(sqlOptions.Sql, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var writeFilters = context.Options.ToObject<WriteOptions>();
        await _manager.WriteAsync(sqlOptions.Sql, writeFilters, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();
        return await _manager.ReadAsync(sqlOptions.Sql, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();
        await _manager.DeleteAsync(sqlOptions.Sql, deleteOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        return await _manager.ExistAsync(sqlOptions.Sql, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var sqlOptions = context.Options.ToObject<SqlOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        return await _manager.EntitiesAsync(sqlOptions.Sql, listOptions, cancellationToken);
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}