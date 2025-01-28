using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.Google.Cloud.Services;

public interface IGoogleCloudManager
{
    Task<object> About(Context context, CancellationToken cancellationToken = default);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> ReadAsync(Context context, CancellationToken cancellationToken);

    Task UpdateAsync(Context context, CancellationToken cancellationToken = default);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken);

    Task TransferAsync(Context context, CancellationToken cancellationToken);

    //Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
    //    TransferKind transferKind, CancellationToken cancellationToken);

    //Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}