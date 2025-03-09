using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Database.MySql.Services;

public interface IMysqlDatabaseManager
{
    Task<Result> Create(Context context, CancellationToken cancellationToken);

    Task<Result> Write(Context context, CancellationToken cancellationToken);

    Task<Result<InterchangeData>> Read(Context context, CancellationToken cancellationToken);

    Task<Result> Update(Context context, CancellationToken cancellationToken);

    Task<Result> Delete(Context context, CancellationToken cancellationToken);

    Task<Result<bool>> Exist(Context context, CancellationToken cancellationToken);

    Task<Result<InterchangeData>> Entities(Context context, CancellationToken cancellationToken);

    Task<Result> Transfer(Context context, CancellationToken cancellationToken);

    //Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken);

    //Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<Result<IEnumerable<CompressEntry>>> Compress(Context context, CancellationToken cancellationToken);
}