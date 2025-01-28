using System.Data;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Stream.Json.Services;

public interface IJsonManager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> ReadAsync(Context context, CancellationToken cancellationToken);

    Task UpdateAsync(Context context, CancellationToken cancellationToken);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken);

    Task TransferAsync(Context context, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressAsync(Context context, CancellationToken cancellationToken);
}