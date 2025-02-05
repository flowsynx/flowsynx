using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<Result<object>> About(Context context);

    Task<Result> Create(Context context);

    Task<Result> Write(Context context);

    Task<Result<InterchangeData>> Read(Context context);

    Task<Result> Rename(Context context);

    Task<Result> Delete(Context context);

    Task<Result<bool>> Exist(Context context);

    Task<Result<InterchangeData>> FilteredEntities(Context context);

    Task<Result> Transfer(Context context, CancellationToken cancellationToken);

    //Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    Task<Result<IEnumerable<CompressEntry>>> Compress(Context context, CancellationToken cancellationToken);
}