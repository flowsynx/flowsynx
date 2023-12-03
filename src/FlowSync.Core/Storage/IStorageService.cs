using FlowSync.Abstractions.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Storage;

internal interface IStorageService
{
    Task<StorageUsage> About(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageEntity>> List(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, int? maxResult, CancellationToken cancellationToken = default);
    Task Delete(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default);
    Task<StorageStream> ReadAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
}