using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public interface IAzureBlobManager
{
    Task CreateAsync(string path, CreateOptions options, CancellationToken cancellationToken);

    Task WriteAsync(string path, WriteOptions options, object dataOptions, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(string path, ReadOptions options, CancellationToken cancellationToken);

    Task DeleteAsync(string path, CancellationToken cancellationToken);

    Task PurgeAsync(string path, CancellationToken cancellationToken);

    Task<bool> ExistAsync(string path, CancellationToken cancellationToken);

    Task<IEnumerable<StorageEntity>> EntitiesAsync(string path, ListOptions listOptions,
        CancellationToken cancellationToken);

    Task<IEnumerable<object>> FilteredEntitiesAsync(string path, ListOptions listOptions,
        CancellationToken cancellationToken);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path, ListOptions listOptions,
        ReadOptions readOptions, CancellationToken cancellationToken = default);
}