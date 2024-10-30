using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<object> GetStatisticsAsync();

    Task CreateAsync(string path, CreateOptions options);

    Task WriteAsync(string path, WriteOptions options, object dataOptions);

    Task<ReadResult> ReadAsync(string path, ReadOptions options);

    Task DeleteAsync(string path);

    Task PurgeAsync(string path);

    Task<bool> ExistAsync(string path);

    Task<IEnumerable<StorageEntity>> EntitiesAsync(string path, ListOptions listOptions);

    Task<IEnumerable<object>> FilteredEntitiesAsync(string path, ListOptions listOptions);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, string path, 
        ListOptions listOptions, ReadOptions readOptions);
}