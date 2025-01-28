using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public interface IMemoryManager
{
    Task<object> About(Context context);

    Task CreateAsync(Context context);

    Task WriteAsync(Context context);

    Task<InterchangeData> ReadAsync(Context context);

    Task UpdateAsync(Context context);

    Task DeleteAsync(Context context);

    Task<bool> ExistAsync(Context context);

    Task<InterchangeData> FilteredEntitiesAsync(Context context);

    Task TransferAsync(Context context, CancellationToken cancellationToken);

    //Task ProcessTransferAsync(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}