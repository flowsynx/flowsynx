using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Database.MySql.Services;

public interface IMysqlDatabaseManager
{
    Task Create(Context context, CancellationToken cancellationToken);

    Task Write(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> Read(Context context, CancellationToken cancellationToken);

    Task Update(Context context, CancellationToken cancellationToken);

    Task Delete(Context context, CancellationToken cancellationToken);

    Task<bool> Exist(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> Entities(Context context, CancellationToken cancellationToken);

    Task Transfer(Context context, CancellationToken cancellationToken);

    //Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken);

    //Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}