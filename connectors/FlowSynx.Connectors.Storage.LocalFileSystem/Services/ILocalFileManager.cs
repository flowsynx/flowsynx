using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<object> GetStatisticsAsync();

    Task CreateAsync(string entity, CreateOptions options);

    Task WriteAsync(string entity, WriteOptions options, object dataOptions);

    Task<ReadResult> ReadAsync(string entity, ReadOptions options);

    Task DeleteAsync(string entity);

    Task PurgeAsync(string entity);

    Task<bool> ExistAsync(string entity);

    Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions listOptions);

    Task<IEnumerable<object>> FilteredEntitiesAsync(string entity, ListOptions listOptions);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string entity, 
        ListOptions listOptions, ReadOptions readOptions);
}