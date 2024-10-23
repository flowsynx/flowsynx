using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IAmazonS3Manager
{
    Task CreateAsync(string entity, CreateOptions options, CancellationToken cancellationToken);
    Task WriteAsync(string entity, WriteOptions options, object dataOptions, CancellationToken cancellationToken);
    Task<ReadResult> ReadAsync(string entity, ReadOptions options, CancellationToken cancellationToken);
    Task DeleteAsync(string entity, CancellationToken cancellationToken);
    Task PurgeAsync(string entity, CancellationToken cancellationToken);
    Task<bool> ExistAsync(string entity, CancellationToken cancellationToken);
    Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions listOptions, CancellationToken cancellationToken);
}