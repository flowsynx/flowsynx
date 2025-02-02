using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.Data;
using FlowSynx.IO.Compression;
using System.Data;

namespace FlowSynx.Connectors.Stream.Csv.Services;

public interface ICsvManager
{
    Task Create(Context context, CancellationToken cancellationToken);

    Task Write(Context context, CancellationToken cancellationToken);

    //Task<InterchangeData> Read(Context context, CancellationToken cancellationToken);

    Task Update(Context context, CancellationToken cancellationToken);

    Task Delete(Context context, CancellationToken cancellationToken);

    Task<bool> Exist(Context context, CancellationToken cancellationToken);
    
    Task<InterchangeData> FilteredEntities(Context context, CancellationToken cancellationToken);

    Task Transfer(Context context, CancellationToken cancellationToken);

    //Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}