using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileBrowser
{
    Task CreateAsync(string entity, CreateOptions options);
    Task WriteAsync(string entity, WriteOptions options, object dataOptions);
    Task<ReadResult> ReadAsync(string entity, ReadOptions options);
    Task DeleteAsync(string entity);
    Task PurgeAsync(string entity);
    Task<bool> ExistAsync(string entity);
    Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions listOptions);
}