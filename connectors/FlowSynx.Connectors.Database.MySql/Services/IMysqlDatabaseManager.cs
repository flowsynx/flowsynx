using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Services;

public interface IMysqlDatabaseManager
{
    Task CreateAsync(string sql, CreateOptions options, CancellationToken cancellationToken);

    Task WriteAsync(string sql, WriteOptions options, object dataOptions, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(string sql, ReadOptions options, CancellationToken cancellationToken);

    Task DeleteAsync(string sql, DeleteOptions options, CancellationToken cancellationToken);

    Task PurgeAsync(string sql, CancellationToken cancellationToken);

    Task<bool> ExistAsync(string sql, CancellationToken cancellationToken);

    Task<IEnumerable<object>> EntitiesAsync(string sql, ListOptions listOptions,
        CancellationToken cancellationToken);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, SqlOptions sqlOptions, 
        ReadOptions readOptions, CancellationToken cancellationToken = default);
}