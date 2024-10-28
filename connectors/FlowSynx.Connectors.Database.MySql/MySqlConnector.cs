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

    public override Task<object> About(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task CreateAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var createOptions = options.ToObject<CreateOptions>();
        await _manager.CreateAsync(context.Entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options,
        object dataOptions, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var writeFilters = options.ToObject<WriteOptions>();
        var queryOption = options.ToObject<QueryOptions>();
        await _manager.WriteAsync(queryOption.Sql, writeFilters, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var readOptions = options.ToObject<ReadOptions>();
        return await _manager.ReadAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var deleteOptions = options.ToObject<DeleteOptions>();
        await _manager.DeleteAsync(context.Entity, deleteOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        return await _manager.ExistAsync(context.Entity, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new DatabaseException("Resources.CalleeConnectorNotSupported");

        var queryOptions = options.ToObject<QueryOptions>();
        return await _manager.EntitiesAsync(queryOptions.Sql, queryOptions, cancellationToken);
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}