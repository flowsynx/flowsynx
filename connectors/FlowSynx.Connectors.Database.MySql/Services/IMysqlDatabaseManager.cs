using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Database.MySql.Services;

public interface IMysqlDatabaseManager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken);

    Task UpdateAsync(Context context, CancellationToken cancellationToken);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task PurgeAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);

    Task<IEnumerable<object>> EntitiesAsync(Context context, CancellationToken cancellationToken);

    Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken);

    Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}