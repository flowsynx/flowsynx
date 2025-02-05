using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.Connectors.Database.MySql.Services;
using FlowSynx.Data;
using FlowSynx.Data.Sql.Builder;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using System.Threading;
using FlowSynx.Abstractions;

namespace FlowSynx.Connectors.Database.MySql;

public class MySqlConnector : Connector
{
    private readonly ILogger<MySqlConnector> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly ISqlBuilder _sqlBuilder;
    private readonly IMySqlDatabaseConnection _connection;
    private IMysqlDatabaseManager _manager = null!;
    private MySqlpecifications _mysqlSpecifications = null!;

    public MySqlConnector(ILogger<MySqlConnector> logger, ISerializer serializer, 
        IDeserializer deserializer, ISqlBuilder sqlBuilder)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _serializer = serializer;
        _deserializer = deserializer;
        _sqlBuilder = sqlBuilder;
        _connection = new MySqlDatabaseConnection();
    }

    public override Guid Id => Guid.Parse("20fc1d0c-7fef-43e4-95bc-f99f3e5fd088");
    public override string Name => "MySql";
    public override Namespace Namespace => Namespace.Connectors;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override System.Type SpecificationsType => typeof(MySqlpecifications);

    public override Task Initialize()
    {
        _mysqlSpecifications = Specifications.ToObject<MySqlpecifications>();
        var connection = _connection.Connect(_mysqlSpecifications);
        _manager = new MysqlDatabaseManager(_logger, connection, _serializer, _deserializer, _sqlBuilder);
        return Task.CompletedTask;
    }

    public async Task<Result> Create(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Create(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result> Write(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Write(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result<InterchangeData>> Read(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Read(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result> Update(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Update(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result> Delete(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Delete(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result<bool>> Exist(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Exist(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result<InterchangeData>> List(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.Entities(context, cancellationToken).ConfigureAwait(false);

    public async Task<Result> Transfer(Context context, CancellationToken cancellationToken = default) =>
        await _manager.Transfer(context, cancellationToken).ConfigureAwait(false);

    //public override async Task TransferAsync(Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, transferKind, 
    //        cancellationToken).ConfigureAwait(false);

    //public override async Task ProcessTransferAsync(Context context, TransferData transferData,
    //    TransferKind transferKind, CancellationToken cancellationToken = default) =>
    //    await _manager.ProcessTransferAsync(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public async Task<Result<IEnumerable<CompressEntry>>> Compress(Context context,
        CancellationToken cancellationToken = default) =>
        await _manager.Compress(context, cancellationToken).ConfigureAwait(false);
}