using System.Data;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Stream.Json.Services;

public interface IJsonManager
{
    Task Create(Context context, CancellationToken cancellationToken);

    Task Write(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> Read(Context context, CancellationToken cancellationToken);

    Task Update(Context context, CancellationToken cancellationToken);

    Task Delete(Context context, CancellationToken cancellationToken);

    Task<bool> Exist(Context context, CancellationToken cancellationToken);

    Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken);

    Task Transfer(Context context, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}