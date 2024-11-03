using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<object> About(Context context);

    Task CreateAsync(Context context);

    Task WriteAsync(Context context);

    Task<ReadResult> ReadAsync(Context context);

    Task UpdateAsync(Context context);

    Task DeleteAsync(Context context);

    Task<bool> ExistAsync(Context context);

    Task<IEnumerable<object>> FilteredEntitiesAsync(Context context);

    Task TransferAsync(Namespace @namespace, string type, Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken);

    Task ProcessTransferAsync(Context context, TransferData transferData, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}