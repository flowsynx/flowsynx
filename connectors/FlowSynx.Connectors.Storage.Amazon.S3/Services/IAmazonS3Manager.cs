using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IAmazonS3Manager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, object dataOptions, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken);

    Task UpdateAsync(Context context, CancellationToken cancellationToken);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);

    Task<IEnumerable<StorageEntity>> EntitiesAsync(Context context, CancellationToken cancellationToken);

    Task<IEnumerable<object>> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken);

    Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken);

    Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}