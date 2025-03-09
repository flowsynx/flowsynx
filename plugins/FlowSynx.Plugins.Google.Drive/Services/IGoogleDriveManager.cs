using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.Google.Drive.Services;

public interface IGoogleDriveManager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task Create(Context context, CancellationToken cancellationToken);

    Task Write(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> Read(Context context, CancellationToken cancellationToken);

    Task Update(Context context, CancellationToken cancellationToken = default);

    Task Delete(Context context, CancellationToken cancellationToken);

    Task<bool> Exist(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken);

    Task Transfer(Context context, CancellationToken cancellationToken);

    //Task Transfer(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken);

    //Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}