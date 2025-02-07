using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<object> About(Context context);

    Task Create(Context context);

    Task Write(Context context);

    Task<InterchangeData> Read(Context context);

    Task Rename(Context context);

    Task Delete(Context context);

    Task<bool> Exist(Context context);

    Task<InterchangeData> FilteredEntities(Context context);

    Task Transfer(Context context, CancellationToken cancellationToken);

    //Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}