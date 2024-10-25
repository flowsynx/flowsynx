using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.Azure.Blobs.Services;

public interface IAzureBlobManager
{
    Task CreateAsync(string entity, CreateOptions options, CancellationToken cancellationToken);

    Task WriteAsync(string entity, WriteOptions options, object dataOptions, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(string entity, ReadOptions options, CancellationToken cancellationToken);

    Task DeleteAsync(string entity, CancellationToken cancellationToken);

    Task PurgeAsync(string entity, CancellationToken cancellationToken);

    Task<bool> ExistAsync(string entity, CancellationToken cancellationToken);

    Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions listOptions,
        CancellationToken cancellationToken);

    Task<IEnumerable<object>> FilteredEntitiesAsync(string entity, ListOptions listOptions,
        CancellationToken cancellationToken);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string entity, ListOptions listOptions,
        ReadOptions readOptions, CancellationToken cancellationToken = default);
}