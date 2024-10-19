using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IAmazonS3Browser
{
    Task CreateAsync(string entity, CreateOptions options, CancellationToken cancellationToken);
    Task WriteAsync(string entity, WriteOptions options, object dataOptions, CancellationToken cancellationToken);
    Task<ReadResult> ReadAsync(string entity, ReadOptions options, CancellationToken cancellationToken);
    Task DeleteAsync(string path, CancellationToken cancellationToken);
    Task<bool> ExistAsync(string entity, CancellationToken cancellationToken);
    Task<IEnumerable<StorageEntity>> ListAsync(string path, ListOptions listOptions, CancellationToken cancellationToken);
}